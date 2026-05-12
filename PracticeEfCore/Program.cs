using System;

namespace PracticeEfCore
{
    public class Program
    {
        EmployeeContext employeeContext;

        public Program()
        {
            employeeContext = new EmployeeContext();
            employeeContext.Database.EnsureCreated();
        }
        public void AddEmployee()
        {
            employeeContext.Employees.Add(new Employee
            {
                FirstName = "Sri Manikandan",
                LastName = "R",
                Email = "srimanikandandev@gmail.com",
                Phone = "9677741597",
                HireDate = DateTime.UtcNow,
                Salary = 25000
            });
            employeeContext.SaveChanges();
        }

        void GetEmployees()
        {
            var employees = employeeContext.Employees;
            foreach(var emp in employees)
            {
                Console.WriteLine(emp.FirstName + " " + emp.LastName + " " + emp.Email + " " + emp.Phone + " " + emp.HireDate + " " + emp.Salary);
            }
        }

        void UpdateEmployee()
        {
            Employee employee = employeeContext.Employees.Find(1);
            employee.Salary = 30000;
            employeeContext.Employees.Update(employee);
            employeeContext.SaveChanges();
        }

        static void Main(string[] args)
        {
            Program program = new Program();
            program.AddEmployee();
            program.GetEmployees();
            program.UpdateEmployee();
            program.GetEmployees();
        }
    }
}