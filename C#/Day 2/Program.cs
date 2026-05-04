using UnderstandingOOPSApp.Interfaces;
using UnderstandingOOPSApp.Services;

namespace UnderstandingOOPSApp
{
    internal class Program
    {
        ICustomerInteract customerInteract;
        public Program()
        {
            customerInteract = new CustomerService();
        }
        void DoBanking()
        {
            while (true)
            {
                Console.WriteLine("\n===== Banking Menu =====");
                Console.WriteLine("1. Add Account");
                Console.WriteLine("2. Print Account Details by Account Number");
                Console.WriteLine("3. Print Account Details by Phone Number");
                Console.WriteLine("4. Exit");
                Console.Write("Enter your choice: ");

                string input = Console.ReadLine() ?? "";
                if (!int.TryParse(input, out int choice))
                {
                    Console.WriteLine("Invalid choice. Please enter 1, 2, 3, or 4.");
                    continue;
                }

                switch (choice)
                {
                    case 1:
                        var account = customerInteract.OpensAccount();
                        Console.WriteLine("\nAccount created successfully!");
                        Console.WriteLine(account);
                        break;
                    case 2:
                        Console.Write("Enter Account Number: ");
                        string accNum = Console.ReadLine() ?? "";
                        customerInteract.PrintAccountDetails(accNum);
                        break;
                    case 3:
                        Console.Write("Enter Phone Number: ");
                        string phone = Console.ReadLine() ?? "";
                        customerInteract.PrintAccountDetailsByPhone(phone);
                        break;
                    case 4:
                        Console.WriteLine("Thank you for banking with us. Goodbye!");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1, 2, 3, or 4.");
                        break;
                }
            }
        }
        static void Main(string[] args)
        {
            new Program().DoBanking();
        }
    }
}