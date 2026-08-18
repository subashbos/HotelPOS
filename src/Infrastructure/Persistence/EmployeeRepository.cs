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
                .AsNoTracking()
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

        public async Task<Employee?> GetByUserIdAsync(int userId)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.UserId == userId);
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
            return await _context.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
        }

        public async Task SaveDepartmentAsync(Department department)
        {
            if (department.Id == 0)
                _context.Departments.Add(department);
            else
                _context.Departments.Update(department);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDepartmentAsync(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept != null)
            {
                _context.Departments.Remove(dept);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Designation>> GetDesignationsAsync()
        {
            return await _context.Designations.AsNoTracking().Include(d => d.Department).OrderBy(d => d.Title).ToListAsync();
        }

        public async Task SaveDesignationAsync(Designation designation)
        {
            if (designation.Id == 0)
                _context.Designations.Add(designation);
            else
                _context.Designations.Update(designation);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDesignationAsync(int id)
        {
            var desig = await _context.Designations.FindAsync(id);
            if (desig != null)
            {
                _context.Designations.Remove(desig);
                await _context.SaveChangesAsync();
            }
        }
    }
}
