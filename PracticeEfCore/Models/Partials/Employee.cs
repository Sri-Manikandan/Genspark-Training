using System;

namespace PracticeEfCore
{
    public partial class Employee
    {
        public override string ToString()
        {
            return $"Employee ID: {EmployeeId}, Name: {FirstName} {LastName}, Email: {Email}, Phone: {Phone}, Hire Date: {HireDate}, Salary: {Salary}";
        }
    }
}
