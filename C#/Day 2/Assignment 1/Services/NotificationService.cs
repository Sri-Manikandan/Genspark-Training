using System;
using System.Collections.Generic;
using Sns.Models;
using Sns.Interfaces;

namespace Sns.Services{
    internal class NotificationService : INotificationService{
        private List<User> users = new List<User>();

        public void createUser(string name, string email, string phoneNumber){
            users.Add(new User(name, email, phoneNumber));
            Console.WriteLine("User created: " + name);
        }

        public void sendMessage(string message){
            if(users.Count == 0){
                Console.WriteLine("No users to notify.");
                return;
            }
            foreach(var user in users){
                new EmailNotification(DateTime.Now, message, user.Name);
                new SmsNotification(DateTime.Now, message, user.Name);
            }
        }
        public void listUsers(){
            foreach(var user in users){
                Console.WriteLine(user.ToString());
            }
        }
    }
}
