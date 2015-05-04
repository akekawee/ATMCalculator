using System;
using System.Linq;
using ATMcalculator.Logic;

namespace ATMcalculator
{
    class Program
    {
        private static readonly int[] CashDenoms = { 5000, 2000, 1000, 500, 200, 100, 20, 20, 10, 5 };
        private static readonly int[] CashLimits100 = { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 };

        static void Main()
        {
            var cca = new CalculatorAlg();
            decimal amount = 0m;
            for (int i = 1; i <= 300; i++)
            {
                decimal request = (10m + amount);
                int target = (int)Math.Truncate(request * 100);
                var cashChange = cca.FindOptimalChange(target, CashDenoms, CashLimits100);
                Console.WriteLine("Amount Req: ${0}", request);
                foreach (var s in cashChange.Denominations.Where(c => c.Value != 0))
                {
                    Console.WriteLine("Denom ${0} ==> {1}", s.Key / 100, s.Value);
                }
                Console.WriteLine("Amount Remaining: ${0}", cashChange.Remaining / 100);
                amount += 5m;
                Console.WriteLine("===========================");
                if (i % 10 == 0)
                {
                    Console.ReadKey();
                    Console.Clear();
                }
            }
            Console.ReadKey();
        }
    }
}
