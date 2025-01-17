using ArithmeticDemo.Sort.interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArithmeticDemo.Sort
{
    /// <summary>
    /// 插入排序
    /// </summary>
    public class InsertionSort : ISort
    {
        public int[] sort(int[] arr)
        {
            for (int i = 1; i < arr.Length; i++) 
            {
                var temp = arr[i];
                var k = i - 1;
                while (k >= 0 && temp < arr[k])
                    k--;
                for (int j = i; j > k+1; j--)
                {
                    arr[j] = arr[j - 1];
                }
                arr[k+1] = temp;
            }
            return arr;
        }
    }
}
