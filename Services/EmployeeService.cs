using Microsoft.EntityFrameworkCore;
using CafeteriaSystem.Data;
using CafeteriaSystem.Models;


namespace CafeteriaSystem.Services
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetAllEmployeesAsync();
        Task<Employee> GetEmployeeByIdAsync(int id);
        Task<(bool Success, string ErrorMessage, int EmployeeId)> ProcessDepositAsync(string employeeNumber, decimal amount, string userId);
        Task AddEmployeeAsync(Employee employee);
    }

    public class EmployeeService : IEmployeeService
    {

        private readonly ApplicationDbContext _context;

        public EmployeeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees.ToListAsync();
        }

        public async Task<Employee> GetEmployeeByIdAsync(int id)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return await _context.Employees.FindAsync(id);
#pragma warning restore CS8603 // Possible null reference return.
        }

        public async Task AddEmployeeAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task<(bool Success, string ErrorMessage, int EmployeeId)> ProcessDepositAsync(string employeeNumber, decimal amount, string userId)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber && e.UserId == userId);
            if (employee == null)
                return (false, "Employee not found.", 0);

            var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            decimal totalDepositedThisMonth = amount;

            // Check if this is a new month
            if (employee.LastDepositMonth.Year != currentMonth.Year || employee.LastDepositMonth.Month != currentMonth.Month)
            {
                employee.LastDepositMonth = currentMonth;
            }

            // Bonus logic R500 bonus for every R250 deposited
            int bonusIncrements = (int)(totalDepositedThisMonth / 250);
            decimal bonus = bonusIncrements * 500;

            employee.Balance += amount + bonus;
            await _context.SaveChangesAsync();

            return (true, "", employee.Id);
        }
    }
}
