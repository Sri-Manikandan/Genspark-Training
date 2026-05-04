using System;
using Sns.Services;
using Sns.Models;
using Sns.Interfaces;

namespace Sns{
    class Program{

        static void Main(string[] args){
            NotificationService notificationService = new NotificationService();
            while(true){
                Console.WriteLine("1. Create User");
                Console.WriteLine("2. Send Message");
                Console.WriteLine("3. List Users");
                Console.WriteLine("4. Exit");
                int userChoice = Convert.ToInt32(Console.ReadLine());
                switch(userChoice){
                    case 1:
                        Console.WriteLine("Enter Name : ");
                        string name = Console.ReadLine() ?? string.Empty;
                        Console.WriteLine("Enter Email : ");
                        string email = Console.ReadLine() ?? string.Empty;
                        Console.WriteLine("Enter Phone Number : ");
                        string phoneNumber = Console.ReadLine() ?? string.Empty;
                        notificationService.createUser(name, email, phoneNumber);
                        break;
                    case 2:
                        Console.WriteLine("Enter Message : ");
                        string message = Console.ReadLine() ?? string.Empty;
                        notificationService.sendMessage(message);
                        break;
                    case 3:
                        notificationService.listUsers();
                        break;
                    case 4:
                        return;
                    default:
                        Console.WriteLine("Invalid Choice");
                        break;
                }
            }
        }
    }
}
