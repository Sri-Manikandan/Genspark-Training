using System;

namespace Day_4
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                checked
                {
                    int num1 = int.MaxValue;
                    num1--; num1++;
                    Console.WriteLine("The updated value is " + num1);
                    Console.WriteLine("Now you can enter a number");
                    num1 = Convert.ToInt32(Console.ReadLine());
                    Console.WriteLine("Please enter the dinominator");
                    int num2 = Convert.ToInt32(Console.ReadLine());
                    var result = num1 / num2;
                    Console.WriteLine("The final result is " + result);
                }
            }
            catch(OverflowException ofe)
            {
                Console.WriteLine(ofe.Message);//for programmer
                Console.WriteLine("Sorry the data could not be saved. Please start over");//end user
            }
            catch(FormatException fe)
            {
                Console.WriteLine(fe.Message);
                Console.WriteLine("The input you gave was not a number. We are expectecting a whole number");
            }
            catch(DivideByZeroException dbze)
            {
                Console.WriteLine(dbze.Message);
                Console.WriteLine("Opps unfortunate number for a division. cannot proceed further.");
            }
            catch (Exception ex) 
            { 
                Console.WriteLine(ex.Message);
                Console.WriteLine("Sorry something went wrong");
            }
            Console.WriteLine("Bye bye");
        } 
    }
}