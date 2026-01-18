using System;
using System.Collections.Generic;
using System.Text;

namespace RestX.BLL
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string AzureStorageAccountName { get; set; }
        public string AzureStorageAccountKey { get; set; }
        public string MandrillKey { get; set; }
        public string GoldMedalUsername { get; set; }
        public string GoldMedalPassword { get; set; }
		public string GoldMedalFlightSearchUsername { get; set; }
		public string GoldMedalFlightSearchPassword { get; set; }
		public string GoldMedalFlightSearchAPIKey { get; set; }
		public string BuildNumber { get; set; }
        public string TenantConnectionStringTemplate {get;set;}
        public string CosmosDbContentEndpoint { get; set; }
        public string CosmosDbContentKey { get; set; }
        public string CosmosDbContentDatabaseId { get; set; }
        public string HotelBedsApiKey { get; set; }
        public string HotelBedsApiSecret { get; set; }
        public string HotelBedsUrl { get; set; }
        public string GetAddressApiKey { get; set; }
        public bool DebugTravelink { get; set; }
        public bool DebugCruiseSearch { get; set; }
        public string SendGridApiKey { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string SqlAzureElasticPool { get; set; }
        public string TenantIdFilesToCopyForNewTenant { get; set; }
        public bool EnableDestinationCaching { get; set; }
        public string TwoFactorAuthTwilioAccountSid { get; set; }
        public string TwoFactorAuthTwilioAuthToken { get; set; }
        public string TwoFactorAuthTwilioFromNumber { get; set; }
        public string PdfCreationUrl { get; set; }
        public bool TravelinkUseProxy { get; set; }
        public bool UseLargeCacheLogic { get; set; }
        public int MinimumWorkerThreads { get; set; }
        public int MinimumIoThreads { get; set; }
		public string AzureOpenAIKey { get; set; }
		public string AzureOpenAIEndpoint { get; set; }
		public string AzureOpenAIQuotesKey { get; set; }
		public string AzureOpenAIQuotesEndpoint { get; set; }
		public string AzureQuoteAISearchKey { get; set; }
		public string AzureQuoteAISearchEndpoint { get; set; }		
		public string AzureQuoteAISearchIndexName { get; set; }
        public string FlightLabsAccessKey { get; set; }
        public string AzureSpeechKey { get; set; }
        public string AzureSpeechRegion { get; set; }
        public string AzureSpeechVoice { get; set; }
        public string RecognitionLanguage { get; set; }
        public string ExchangeRateEndPoint { get; set; }
        public string ExchangeRateApiKey { get; set; }
        public string WeatherEndPoint { get; set; }
        public string WeatherApiKey { get; set; }
        public string PaxportUsername { get; set; }
        public string PaxportPassword { get; set; }
        public string ExternalApiKey { get; set; }
        public string FontAwesomeApiKey { get; set; }
        public string CiriumAppId { get; set; }
        public string CiriumAppKey { get; set; }
        public string CiriumHookApiUrl { get; set; }
        public string Jet2HolidaysUrl { get; set; }
        public string Jet2HolidaysUsername { get; set; }
        public string Jet2HolidaysPassword { get; set; }

        // For AI
        public string AILeadSenderEmail { get; set; }
        public string AIDocumentInboundEmail { get; set; }
        public string AILeadTwilioPhoneNumber { get; set; }
        public int ProfilingTriggerDebounceTime { get; set; }
        public string WhatsappSignupUrl { get; set; }
        public string WhatsappAppId { get; set; }
        public string WhatsappConfigId { get; set; }
        public string WhatsappApiVersion { get; set; }
        public string WhatsappGraphApiUrl { get; set; }
        public string WhatsappSecret { get; set; }
        public string SearchServiceEndPoint { get; set; }
        public string SearchServiceAdminApiKey { get; set; }

        // SystemsX Settings
        public string SystemsXApiUrl { get; set; }
        public string SystemsXApiKey { get; set; }
    }
}
