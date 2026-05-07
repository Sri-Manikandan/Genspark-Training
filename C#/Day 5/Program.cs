using System;

namespace Day5{

internal class Program
    {
        // public delegate void MyDelegate(int n1, int n2);//Declare the type

        public delegate void MyDelegate<T,K>(T n1, K n2);

        public Action<int,int> delegateRef;//refference for the type
        //MyDelegate<int, int> del;

        public void Add(int num1, int num2)//Method that could be delegated
        {
            var result = num1 + num2;
            Console.WriteLine($"The sum of {num1} and {num2} is {result}");
        }

        public void Product(int num1, int num2)//Method that could be delegated
        {
            var result = num1 * num2;
            Console.WriteLine($"The product of {num1} and {num2} is {result}");
        }

        public Program()//Constructore for instan
        {
            delegateRef = new Action<int,int>(Product);
            //delegateRef += delegate (int num1, int num2) //anon method
            //{
            //    var result = num1 + num2;
            //    Console.WriteLine($"The sum of {num1} and {num2} is {result}");
            //};

            delegateRef += (num1, num2)=> Console.WriteLine($"The sum of {num1} and {num2} is {(num1+num2)}");

            delegateRef -= Product;
        }

        void Calculate(Action<int,int> del) //takes functionality as parameter
        {
            del(100, 200);
        }

        static void Main(string[] args)
        {
            Program program = new Program();
            program.Calculate(program.delegateRef);
        }
    }
}
