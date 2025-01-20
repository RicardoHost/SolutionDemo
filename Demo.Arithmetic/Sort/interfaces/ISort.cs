using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Arithmetic.Sort.interfaces
{
    public interface ISort
    {
        int[] sort(int[] arr);

        public static void swap(int[] arr,int i,int j)
        {
            var temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }
    }
}
