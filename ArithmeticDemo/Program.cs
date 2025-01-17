using ArithmeticDemo.Sort;
using ArithmeticDemo.Sort.interfaces;
using Microsoft.VisualBasic;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace ArithmeticDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Sort();
        }

        public static void Sort()
        {
            var random = new Random();
            var arr = Enumerable.Range(1, 10000).Select(it => random.Next(10000)).ToArray() ;
            ISort sort = new InsertionSort();
            
            var start= DateTime.Now.Ticks;
            arr = sort.sort(arr);
            var end = DateTime.Now.Ticks;
            Console.WriteLine(end - start);
            Console.WriteLine(string.Join(",", arr));
        }
    }
}
