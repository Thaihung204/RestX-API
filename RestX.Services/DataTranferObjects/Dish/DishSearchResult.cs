using RestX.BLL.DataTranferObjects.Dish;

namespace RestX.BLL.DataTranferObjects.Dish
{
    public class DishSearchResult
    {
        public List<DishItem> Dishes { get; set; } = new();
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalCount { get; set; }
    }
}