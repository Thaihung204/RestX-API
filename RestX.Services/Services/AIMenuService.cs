using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RestX.BLL.DataTranferObjects.AI;
using RestX.BLL.DataTranferObjects.Dish;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Customers;
using RestX.Models.Tenants;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestX.BLL.Services
{
    public class AIMenuService : IAIMenuService
    {
        private readonly IDishService _dishService;
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IRedisService _redisService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ActiveTenant _currentTenant;

        private readonly string _model;
        private readonly int _maxHistoryMessages;
        private readonly int _sessionExpireMinutes;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public AIMenuService(
            IDishService dishService,
            IOrderService orderService,
            ICustomerService customerService,
            IRedisService redisService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IEnumerable<ActiveTenant> tenant)
        {
            _dishService = dishService;
            _orderService = orderService;
            _customerService = customerService;
            _redisService = redisService;
            _httpClientFactory = httpClientFactory;
            _currentTenant = tenant?.FirstOrDefault();

            var aiConfig = configuration.GetSection("AISuggestion");
            _model = aiConfig["Model"] ?? "llama-3.3-70b-versatile";
            _maxHistoryMessages = int.TryParse(aiConfig["MaxHistoryMessages"], out var maxMsg) ? maxMsg : 20;
            _sessionExpireMinutes = int.TryParse(aiConfig["SessionExpireMinutes"], out var expire) ? expire : 30;
        }

        public async Task<AIChatResponse> ChatAsync(AIChatRequest request)
        {
            var sessionId = string.IsNullOrEmpty(request.SessionId)
                ? Guid.NewGuid().ToString()
                : request.SessionId;

            var (history, menu, userPrefs) = await LoadContextAsync(sessionId, request.UserId);
            var systemPrompt = BuildSystemPrompt(menu, request.TableId, userPrefs);

            var rawResponse = await CallGroqAsync(systemPrompt, history, request.Message);
            var (aiResponse, orderAction) = ParseAIResponse(rawResponse, sessionId, menu, request.TableId);

            history.Add(new ChatMessage { Role = "user", Content = request.Message });
            history.Add(new ChatMessage { Role = "assistant", Content = rawResponse });

            if (history.Count > _maxHistoryMessages)
                history = history.Skip(history.Count - _maxHistoryMessages).ToList();

            await SaveHistoryAsync(sessionId, history);

            if (aiResponse.OrderDraft != null && !string.IsNullOrEmpty(request.UserId))
                await SaveUserPrefsAsync(request.UserId, aiResponse.OrderDraft.Items);

            if (orderAction == "create" && aiResponse.OrderDraft != null && request.TableId.HasValue && !string.IsNullOrEmpty(request.UserId))
            {
                aiResponse.CreatedOrderId = await AutoCreateOrderAsync(aiResponse.OrderDraft, request.UserId, request.TableId.Value);
                if (aiResponse.CreatedOrderId.HasValue)
                    aiResponse.OrderDraft = null;
            }

            return aiResponse;
        }

        public async Task ChatStreamAsync(AIChatRequest request, HttpResponse httpResponse)
        {
            var sessionId = string.IsNullOrEmpty(request.SessionId)
                ? Guid.NewGuid().ToString()
                : request.SessionId;

            var (history, menu, userPrefs) = await LoadContextAsync(sessionId, request.UserId);
            var systemPrompt = BuildSystemPrompt(menu, request.TableId, userPrefs);

            httpResponse.ContentType = "text/event-stream";
            httpResponse.Headers["Cache-Control"] = "no-cache";
            httpResponse.Headers["X-Accel-Buffering"] = "no";

            var fullContent = new StringBuilder();

            await foreach (var delta in StreamGroqAsync(systemPrompt, history, request.Message))
            {
                fullContent.Append(delta);
                var sseData = JsonSerializer.Serialize(new { content = delta });
                await httpResponse.WriteAsync($"event: delta\ndata: {sseData}\n\n");
                await httpResponse.Body.FlushAsync();
            }

            var rawResponse = fullContent.ToString();

            try
            {
                var (aiResponse, orderAction) = ParseAIResponse(rawResponse, sessionId, menu, request.TableId);

                history.Add(new ChatMessage { Role = "user", Content = request.Message });
                history.Add(new ChatMessage { Role = "assistant", Content = rawResponse });

                if (history.Count > _maxHistoryMessages)
                    history = history.Skip(history.Count - _maxHistoryMessages).ToList();

                await SaveHistoryAsync(sessionId, history);

                if (aiResponse.OrderDraft != null && !string.IsNullOrEmpty(request.UserId))
                    await SaveUserPrefsAsync(request.UserId, aiResponse.OrderDraft.Items);

                if (orderAction == "create" && aiResponse.OrderDraft != null && request.TableId.HasValue && !string.IsNullOrEmpty(request.UserId))
                {
                    aiResponse.CreatedOrderId = await AutoCreateOrderAsync(aiResponse.OrderDraft, request.UserId, request.TableId.Value);
                    if (aiResponse.CreatedOrderId.HasValue)
                        aiResponse.OrderDraft = null;
                }

                var completeData = JsonSerializer.Serialize(aiResponse);
                await httpResponse.WriteAsync($"event: complete\ndata: {completeData}\n\n");
                await httpResponse.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                var fullError = ex.InnerException?.Message ?? ex.Message;
                var errorData = JsonSerializer.Serialize(new { error = fullError });
                await httpResponse.WriteAsync($"event: error\ndata: {errorData}\n\n");
                await httpResponse.Body.FlushAsync();
            }
        }

        public async Task ClearSessionAsync(string sessionId)
        {
            await _redisService.RemoveAsync(GetSessionKey(sessionId));
        }

        private string BuildSystemPrompt(List<MenuCategory> menu, Guid? tableId, List<string> userPrefs)
        {
            var tenantName = _currentTenant?.Name ?? "nhà hàng";

            var menuText = new StringBuilder();
            foreach (var category in menu)
            {
                menuText.AppendLine($"\n[{category.CategoryName}]");
                foreach (var item in category.Items)
                {
                    var tags = new List<string>();
                    if (item.IsVegetarian) tags.Add("chay");
                    if (item.IsSpicy) tags.Add("cay");
                    if (item.IsBestSeller) tags.Add("bán chạy");

                    var tagStr = tags.Count > 0 ? $" ({string.Join(", ", tags)})" : "";
                    menuText.AppendLine($"- ID: {item.Id} | {item.Name} | {item.Price:N0}đ/{item.Unit}{tagStr}");
                    if (!string.IsNullOrEmpty(item.Description))
                        menuText.AppendLine($"  Mô tả: {item.Description}");
                }
            }

            var tableContext = tableId.HasValue
                ? $"\nKhách đang ngồi tại bàn ID: {tableId}. Khi tạo orderDraft, hãy điền tableId này vào trường tableId."
                : "";

            var prefsContext = userPrefs.Count > 0
                ? $"\nKhách này thường đặt: {string.Join(", ", userPrefs)}. Ưu tiên gợi ý các món tương tự hoặc phù hợp."
                : "";

            return $@"Bạn là trợ lý AI thân thiện của nhà hàng {tenantName}, tên là ""Foody"".
                    Bạn nói chuyện như một người bạn am hiểu ẩm thực — dùng ngôn ngữ tự nhiên, có cảm xúc, đôi khi hài hước nhẹ nhàng.
                    Nhiệm vụ: giúp khách tìm món ngon, gợi ý phù hợp sở thích, và hỗ trợ đặt hàng nhanh gọn.{tableContext}{prefsContext}

                    PHONG CÁCH VIẾT ""message"":
                    - Mở đầu tự nhiên: ""Ồ, lựa chọn tuyệt đấy!"", ""Để Foody gợi ý nhé..."", ""Hôm nay thử cái này xem sao!"", ""Nghe hấp dẫn ghê, để mình tìm cho bạn...""
                    - Dùng ngôn ngữ gần gũi: ""bạn"", ""mình"", ""nha"", ""nhé"", ""đó"", ""á""
                    - Khi confirm đơn: viết tóm tắt tự nhiên kiểu ""Okie! Mình đã đặt cho bạn: 2 Bánh mì thịt nướng + 1 Phở bò tái = 125.000đ. Đơn đang được chuẩn bị nha! 🍜""
                    - Khi upsell: gợi ý nhẹ nhàng, không ép: ""Thêm ly trà sữa cho đủ bộ không? 😄""
                    - Tránh câu cứng nhắc kiểu ""Đã nhận đơn hàng của bạn"", ""Hệ thống đã xử lý""

                    === MENU HIỆN TẠI ===
                    {menuText}
                    === HẾT MENU ===

                    QUY TẮC QUAN TRỌNG:
                    - Luôn trả lời bằng tiếng Việt
                    - LUÔN trả về đúng định dạng JSON sau, không thêm text bên ngoài JSON:
                    {{
                      ""message"": ""Nội dung trả lời tự nhiên, có cảm xúc"",
                      ""suggestions"": [
                        {{
                          ""dishId"": ""uuid-của-món"",
                          ""dishName"": ""Tên món"",
                          ""price"": 45000,
                          ""reason"": ""Lý do gợi ý hấp dẫn, ngắn gọn"",
                          ""category"": ""Tên danh mục""
                        }}
                      ],
                      ""quickReplies"": [""Gợi ý 1"", ""Gợi ý 2"", ""Gợi ý 3""],
                      ""orderDraft"": {{
                        ""tableId"": ""uuid-bàn-hoặc-null"",
                        ""items"": [
                          {{""dishId"": ""uuid"", ""dishName"": ""Tên món"", ""quantity"": 1, ""price"": 45000}}
                        ]
                      }}
                    }}
                    - Chỉ gợi ý món có trong menu, dùng đúng ID từ menu
                    - Gợi ý 1-3 món mỗi lần, phù hợp với yêu cầu của khách
                    - quickReplies là 2-3 câu hỏi/hành động gợi ý tiếp theo, viết tự nhiên như khách đang nói
                    - Nếu không cần gợi ý món, để suggestions là mảng rỗng []
                    - Chỉ tạo orderDraft khi khách RÕ RÀNG muốn đặt món (ví dụ: ""cho tôi 2 phở"", ""đặt món này"", ""order đi"", ""thêm 1 cái nữa"", ""thêm món X""). Nếu chỉ hỏi thăm, để orderDraft là null
                    - Khi tạo orderDraft, đồng thời trả về ""orderAction"": ""create"" và trong ""message"" tóm tắt đơn theo phong cách thân thiện (tên món, số lượng, tổng tiền)
                    - MỖI lần khách muốn đặt thêm (dù đã có đơn trước đó), hãy tạo orderDraft MỚI CHỈ chứa các món khách vừa yêu cầu. KHÔNG gộp với đơn cũ đã tạo trước đó
                    - UPSELL: Khi tạo orderDraft, nếu đơn hàng KHÔNG có đồ uống, gợi ý 1-2 đồ uống/tráng miệng vào ""upsellSuggestions"" và đề cập nhẹ trong message. Nếu đã đủ, để mảng rỗng []

                    JSON format đầy đủ:
                    {{
                      ""message"": ""Nội dung trả lời"",
                      ""suggestions"": [{{""dishId"": ""uuid"", ""dishName"": ""Tên"", ""price"": 45000, ""reason"": ""Lý do"", ""category"": ""Danh mục""}}],
                      ""upsellSuggestions"": [{{""dishId"": ""uuid"", ""dishName"": ""Tên"", ""price"": 15000, ""reason"": ""Gợi ý thêm"", ""category"": ""Đồ uống""}}],
                      ""quickReplies"": [""Gợi ý 1"", ""Gợi ý 2""],
                      ""orderDraft"": {{""tableId"": null, ""items"": [{{""dishId"": ""uuid"", ""dishName"": ""Tên"", ""quantity"": 1, ""price"": 45000}}]}},
                      ""orderAction"": ""create""
                    }}";
        }

        private async Task<string> CallGroqAsync(string systemPrompt, List<ChatMessage> history, string userMessage)
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            var messages = BuildMessages(systemPrompt, history, userMessage);

            var requestBody = new
            {
                model = _model,
                messages,
                max_tokens = 1024,
                temperature = 0.7,
                response_format = new { type = "json_object" }
            };

            var httpContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("openai/v1/chat/completions", httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Groq API error {response.StatusCode}: {errorBody}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }

        private async IAsyncEnumerable<string> StreamGroqAsync(string systemPrompt, List<ChatMessage> history, string userMessage)
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            var messages = BuildMessages(systemPrompt, history, userMessage);

            var requestBody = new
            {
                model = _model,
                messages,
                max_tokens = 1024,
                temperature = 0.7,
                response_format = new { type = "json_object" },
                stream = true
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "openai/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Groq API error {response.StatusCode}: {errorBody}");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

                var data = line["data: ".Length..];
                if (data == "[DONE]") break;

                using var doc = JsonDocument.Parse(data);
                var delta = doc.RootElement.GetProperty("choices")[0].GetProperty("delta");

                if (delta.TryGetProperty("content", out var contentEl))
                {
                    var content = contentEl.GetString();
                    if (!string.IsNullOrEmpty(content))
                        yield return content;
                }
            }
        }

        private static List<object> BuildMessages(string systemPrompt, List<ChatMessage> history, string userMessage)
        {
            var messages = new List<object> { new { role = "system", content = systemPrompt } };
            foreach (var msg in history)
                messages.Add(new { role = msg.Role, content = msg.Content });
            messages.Add(new { role = "user", content = userMessage });
            return messages;
        }

        private (AIChatResponse response, string? orderAction) ParseAIResponse(string rawText, string sessionId, List<MenuCategory> menu, Guid? tableId)
        {
            try
            {
                var start = rawText.IndexOf('{');
                var end = rawText.LastIndexOf('}');

                if (start == -1 || end == -1 || end < start)
                    return (FallbackResponse(sessionId, rawText), null);

                var jsonStr = rawText[start..(end + 1)];
                using var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;

                var response = new AIChatResponse
                {
                    SessionId = sessionId,
                    Message = root.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : rawText
                };

                var menuLookup = menu.SelectMany(c => c.Items).ToDictionary(i => i.Id);

                if (root.TryGetProperty("suggestions", out var suggestionsEl) && suggestionsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in suggestionsEl.EnumerateArray())
                    {
                        if (!s.TryGetProperty("dishId", out var dishIdEl)) continue;
                        if (!Guid.TryParse(dishIdEl.GetString(), out var dishId)) continue;

                        var suggestion = new AISuggestion
                        {
                            DishId = dishId,
                            DishName = s.TryGetProperty("dishName", out var name) ? name.GetString() ?? "" : "",
                            Price = s.TryGetProperty("price", out var price) ? price.GetDecimal() : 0,
                            Reason = s.TryGetProperty("reason", out var reason) ? reason.GetString() ?? "" : "",
                            Category = s.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "",
                        };

                        if (menuLookup.TryGetValue(dishId, out var menuItem))
                            suggestion.ImageUrl = menuItem.ImageUrl;

                        suggestion.Actions = BuildActions(dishId);
                        response.Suggestions.Add(suggestion);
                    }
                }

                if (root.TryGetProperty("quickReplies", out var quickRepliesEl) && quickRepliesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var qr in quickRepliesEl.EnumerateArray())
                    {
                        var text = qr.GetString();
                        if (!string.IsNullOrEmpty(text))
                            response.QuickReplies.Add(text);
                    }
                }

                if (root.TryGetProperty("upsellSuggestions", out var upsellSuggestionsEl) && upsellSuggestionsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in upsellSuggestionsEl.EnumerateArray())
                    {
                        if (!s.TryGetProperty("dishId", out var dishIdEl)) continue;
                        if (!Guid.TryParse(dishIdEl.GetString(), out var dishId)) continue;

                        var upsell = new AISuggestion
                        {
                            DishId = dishId,
                            DishName = s.TryGetProperty("dishName", out var name) ? name.GetString() ?? "" : "",
                            Price = s.TryGetProperty("price", out var price) ? price.GetDecimal() : 0,
                            Reason = s.TryGetProperty("reason", out var reason) ? reason.GetString() ?? "" : "",
                            Category = s.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "",
                        };

                        if (menuLookup.TryGetValue(dishId, out var menuItem))
                            upsell.ImageUrl = menuItem.ImageUrl;

                        upsell.Actions = BuildActions(dishId);
                        response.UpsellSuggestions.Add(upsell);
                    }
                }

                if (root.TryGetProperty("orderDraft", out var draftEl) && draftEl.ValueKind == JsonValueKind.Object)
                {
                    var draft = new AIOrderDraft { TableId = tableId };

                    if (draftEl.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in itemsEl.EnumerateArray())
                        {
                            if (!item.TryGetProperty("dishId", out var itemDishIdEl)) continue;
                            if (!Guid.TryParse(itemDishIdEl.GetString(), out var itemDishId)) continue;

                            draft.Items.Add(new AIOrderDraftItem
                            {
                                DishId = itemDishId,
                                DishName = item.TryGetProperty("dishName", out var dn) ? dn.GetString() ?? "" : "",
                                Quantity = item.TryGetProperty("quantity", out var qty) ? qty.GetInt32() : 1,
                                Price = item.TryGetProperty("price", out var pr) ? pr.GetDecimal() : 0
                            });
                        }
                    }

                    if (draft.Items.Count > 0)
                        response.OrderDraft = draft;
                }

                var orderAction = root.TryGetProperty("orderAction", out var actionEl) ? actionEl.GetString() : null;

                return (response, orderAction);
            }
            catch
            {
                return (FallbackResponse(sessionId, rawText), null);
            }
        }

        private async Task<Guid?> AutoCreateOrderAsync(AIOrderDraft draft, string userId, Guid tableId)
        {
            if (!Guid.TryParse(userId, out var applicationUserId)) return null;

            var customerId = await _customerService.GetCustomerIdByApplicationUserIdAsync(applicationUserId);
            if (!customerId.HasValue) return null;

            var order = new Order
            {
                TableId = tableId,
                CustomerId = customerId.Value,
                TotalAmount = draft.TotalEstimate,
                OrderDetails = draft.Items.Select(i => new OrderDetail
                {
                    DishId = i.DishId,
                    Quantity = i.Quantity
                }).ToList()
            };

            return await _orderService.CreateOrder(order, userId);
        }

        private static List<AIAction> BuildActions(Guid dishId)
        {
            return new List<AIAction>
            {
                new AIAction { Type = "view_detail", Label = "Xem chi tiết", Url = $"/api/dishes/{dishId}" }
            };
        }

        private static AIChatResponse FallbackResponse(string sessionId, string rawText)
        {
            return new AIChatResponse
            {
                SessionId = sessionId,
                Message = rawText,
                Suggestions = new List<AISuggestion>(),
                QuickReplies = new List<string> { "Xem thêm món", "Gợi ý combo", "Tạo đơn hàng" }
            };
        }

        private async Task<(List<ChatMessage> history, List<MenuCategory> menu, List<string> userPrefs)> LoadContextAsync(string sessionId, string? userId)
        {
            var historyTask = LoadHistoryAsync(sessionId);
            var menuTask = LoadMenuCachedAsync();
            var prefsTask = LoadUserPrefsAsync(userId);

            await Task.WhenAll(historyTask, menuTask, prefsTask);

            return (await historyTask, await menuTask, await prefsTask);
        }

        private async Task<List<MenuCategory>> LoadMenuCachedAsync()
        {
            var cacheKey = $"AIMenu:{_currentTenant?.Hostname ?? "default"}";
            var cached = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
                return JsonSerializer.Deserialize<List<MenuCategory>>(cached, _jsonOptions) ?? new List<MenuCategory>();

            var menu = await _dishService.GetMenu();
            await _redisService.SetStringAsync(cacheKey, JsonSerializer.Serialize(menu, _jsonOptions), TimeSpan.FromMinutes(5));
            return menu;
        }

        private async Task<List<ChatMessage>> LoadHistoryAsync(string sessionId)
        {
            var json = await _redisService.GetStringAsync(GetSessionKey(sessionId));
            if (string.IsNullOrEmpty(json)) return new List<ChatMessage>();
            return JsonSerializer.Deserialize<List<ChatMessage>>(json, _jsonOptions) ?? new List<ChatMessage>();
        }

        private async Task SaveHistoryAsync(string sessionId, List<ChatMessage> history)
        {
            var json = JsonSerializer.Serialize(history, _jsonOptions);
            await _redisService.SetStringAsync(GetSessionKey(sessionId), json, TimeSpan.FromMinutes(_sessionExpireMinutes));
        }

        private async Task<List<string>> LoadUserPrefsAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<string>();
            var json = await _redisService.GetStringAsync(GetUserPrefsKey(userId));
            if (string.IsNullOrEmpty(json)) return new List<string>();
            return JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
        }

        private async Task SaveUserPrefsAsync(string userId, List<AIOrderDraftItem> items)
        {
            var key = GetUserPrefsKey(userId);
            var existingJson = await _redisService.GetStringAsync(key);
            var existing = string.IsNullOrEmpty(existingJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(existingJson, _jsonOptions) ?? new List<string>();

            foreach (var item in items)
                if (!existing.Contains(item.DishName))
                    existing.Add(item.DishName);

            if (existing.Count > 10)
                existing = existing.Skip(existing.Count - 10).ToList();

            await _redisService.SetStringAsync(key, JsonSerializer.Serialize(existing, _jsonOptions), TimeSpan.FromDays(30));
        }

        private string GetSessionKey(string sessionId) =>
            $"AIChat:{_currentTenant?.Hostname ?? "default"}:{sessionId}";

        private string GetUserPrefsKey(string userId) =>
            $"AIUserPrefs:{_currentTenant?.Hostname ?? "default"}:{userId}";
    }
}
