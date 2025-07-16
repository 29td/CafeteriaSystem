using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CafeteriaSystem.Data;
using CafeteriaSystem.Models;
using CafeteriaSystem.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CafeteriaSystem.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmployeeService _employeeService; 

        public EmployeesController(ApplicationDbContext context, IEmployeeService employeeService) 
        {
            _context = context;
            _employeeService = employeeService; 
        }

        // List all employees
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            return View(employees);
        }

        // Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                await _employeeService.AddEmployeeAsync(employee);
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public IActionResult Deposit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
            if (employee == null)
            {
                Console.WriteLine($"Employee not found for UserId: {userId}");
                return NotFound("Employee record not found.");
            }

            Console.WriteLine($"Deposit GET: EmployeeNumber={employee.EmployeeNumber}, Balance={employee.Balance}");
            var model = new DepositViewModel
            {
                EmployeeNumber = employee.EmployeeNumber,
                CurrentBalance = employee.Balance,
                DepositAmount = 0
            };
            return View(model);
        }

        [Authorize(Roles = "Employee")]
        [HttpPost]
        public async Task<IActionResult> Deposit(DepositViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
            if (employee == null)
            {
                Console.WriteLine($"Employee not found for UserId: {userId}");
                return NotFound("Employee record not found.");
            }

            model.EmployeeNumber = employee.EmployeeNumber;
            model.CurrentBalance = employee.Balance;

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState errors: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            if (model.DepositAmount <= 0)
            {
                ModelState.AddModelError("DepositAmount", "Deposit amount must be greater than zero.");
                return View(model);
            }

            if (model.EmployeeNumber != employee.EmployeeNumber)
            {
                ModelState.AddModelError("EmployeeNumber", "Invalid employee number.");
                return View(model);
            }

            Console.WriteLine($"Before deposit: Balance={employee.Balance}, DepositAmount={model.DepositAmount}");
            employee.Balance += model.DepositAmount;
            employee.LastDepositMonth = DateTime.Now;
            _context.Employees.Update(employee);

            var depositHistory = new DepositHistory
            {
                EmployeeId = employee.UserId,
                Amount = model.DepositAmount,
                DepositDate = DateTime.Now
            };
            _context.DepositHistories.Add(depositHistory);

            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"After deposit: Balance={employee.Balance}, Deposit saved for EmployeeId={employee.UserId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database save failed: {ex.Message}");
                ModelState.AddModelError("", "Failed to save deposit.");
                return View(model);
            }

            return RedirectToAction("Index", "Home");

        }

        [HttpGet]
        public IActionResult DepositHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = _context.Employees
                .Include(e => e.DepositHistories)
                .FirstOrDefault(e => e.UserId == userId);
            if (employee == null)
            {
                Console.WriteLine($"Employee not found for UserId: {userId}");
                return NotFound("Employee record not found.");
            }

            Console.WriteLine($"DepositHistory GET: EmployeeNumber={employee.EmployeeNumber}, Balance={employee.Balance}, Deposits={employee.DepositHistories.Count}");
            var model = new DepositHistoryViewModel
            {
                EmployeeNumber = employee.EmployeeNumber,
                CurrentBalance = employee.Balance,
                Deposits = employee.DepositHistories.Select(d => new DepositHistoryItem
                {
                    Amount = d.Amount,
                    DepositDate = d.DepositDate
                }).ToList()
            };
            return View(model);
        }

        // Details
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null) return NotFound();
            return View(employee);
        }


    }
}
