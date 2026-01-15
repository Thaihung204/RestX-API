using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Employees;
using RestX.Models.HR;

namespace RestX.BLL.Services
{
    public class EmployeeService : BaseService, IEmployeeService
    {
        private readonly IRepository _repo;

        public EmployeeService(IRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Employee>> GetAllEmployees()
        {
            var employees = await _repo.GetAllAsync<Employee>();
            return employees.ToList();
        }

        public async Task<Employee> GetEmployeeById(Guid id)
        {
            return await _repo.GetByIdAsync<Employee>(id);
        }

        public async Task<Employee> UpsertEmployee(Employee model)
        {
            if (model.Id != Guid.Empty)
            {
                var existing = await _repo.GetByIdAsync<Employee>(model.Id);
                if (existing != null)
                {
                    existing.Code = model.Code;
                    existing.Address = model.Address;
                    existing.Position = model.Position;
                    existing.HireDate = model.HireDate;
                    existing.TerminationDate = model.TerminationDate;
                    existing.Salary = model.Salary;
                    existing.SalaryType = model.SalaryType;
                    existing.IsActive = model.IsActive;

                    _repo.Update(existing);
                    await _repo.SaveAsync();
                    return existing;
                }
            }

            if (model.Id == Guid.Empty)
            {
                model.Id = Guid.NewGuid();
            }

            await _repo.CreateAsync(model);
            return model;
        }

        public async Task DeleteEmployee(Guid id)
        {
            _repo.Delete<Employee>(id);
            await _repo.SaveAsync();
        }
    }
}