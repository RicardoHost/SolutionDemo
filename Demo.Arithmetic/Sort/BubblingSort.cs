using Demo.Arithmetic.Sort.interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Arithmetic.Sort
{
    /// <summary>
    /// 冒泡排序
    /// </summary>
    public class BubblingSort : ISort
    {
        public int[] sort(int[] arr)
        {
            if(arr is null|| arr.Length < 2)
            {
                return arr;
            }
            for (int i = 0; i < arr.Length; i++)
            {
                var flag = false;
                for (int j = 0; j < arr.Length - i - 1; j++)
                {
                    if (arr[j] > arr[j + 1])
                    {
                        flag = true;
                        ISort.swap(arr, j, j + 1);
                    }
                }
                if (!flag) break;
            }
            return arr;
        }
    }
}
