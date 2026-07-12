using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly HotelDbContext _context;

        public EmployeeRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<Employee>> GetAllAsync()
        {
            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.ReportingManager)
                .OrderBy(e => e.FirstName)
                .ToListAsync();
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.ReportingManager)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee?> GetByCodeAsync(string code)
        {
            return await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeCode.ToLower() == code.ToLower());
        }

        public async Task AddAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByCodeAsync(string code, int excludeId = 0)
        {
            return await _context.Employees.AnyAsync(e => e.EmployeeCode.ToLower() == code.ToLower() && e.Id != excludeId);
        }

        public async Task<List<Department>> GetDepartmentsAsync()
        {
            return await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        }

        public async Task<List<Designation>> GetDesignationsAsync()
        {
            return await _context.Designations.Include(d => d.Department).OrderBy(d => d.Title).ToListAsync();
        }

        public async Task<Designation?> GetDesignationByIdAsync(int id)
        {
            return await _context.Designations.Include(d => d.Department).FirstOrDefaultAsync(d => d.Id == id);
        }
    }
}
