namespace RestX.Models.Tenants
{
    public class ActiveTenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ConectionString { get; set; }
    }
}
