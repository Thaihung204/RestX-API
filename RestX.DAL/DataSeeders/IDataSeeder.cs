namespace RestX.DAL.DataSeeders
{
    public interface IDataSeeder
    {
        Task SeedAsync();
        int Order { get; }
    }
}
