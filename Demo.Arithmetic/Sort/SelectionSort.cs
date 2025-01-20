using Demo.Arithmetic.Sort.interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Arithmetic.Sort
{
    /// <summary>
    /// 选择排序
    /// </summary>
    public class SelectionSort : ISort
    {
        public int[] sort(int[] arr)
        {
            if (arr is null || arr.Length < 2)
            {
                return arr;
            }
            return sort1(arr);
        }

        /// <summary>
        /// 基本实现
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public int[] sort1(int[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                for (int j = i + 1; j < arr.Length; j++)
                {
                    if (arr[i] > arr[j])
                    {
                        ISort.swap(arr, i, j);
                    }
                }
            }
            return arr;
        }

        /// <summary>
        /// 通过双指针优化每次比较效率
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public int[] sort2(int[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                var j = i + 1;
                var k = arr.Length - 1;
                while (j <= k)
                {
                    if (arr[i] > arr[j])
                    {
                        ISort.swap(arr, i, j);
                    }
                    if (arr[i] > arr[k])
                    {
                        ISort.swap(arr, i, k);
                    }
                    j++;
                    k--;
                }
            }
            return arr;
        }
    }
}
