using System;
// you can also use other imports, for example:
 using System.Collections.Generic;


class Solution {
    
    public class Seq
    {
        int[] Arr { get;set; }
        int[] Sums { get;set;}
        public double Avg {get;set;}
        public int Start {get;set;}
        public int End {get;set;}
        
        public Seq(int[] arr, int[] sums, int start, int end)
        {
            this.Arr = arr;
            this.Sums = sums;
            
            this.Start = start;
            this.End = end;
            this.Avg = (double)(this.Sums[end+1] - this.Sums[start]) / (end-start+1);
        }
        
        public List<Seq> Split()
        {
            List<Seq> result = new List<Seq>();
            for(int i=Start;i<=End;i++)
            {
                if (Arr[i] > this.Avg)
                {
                    if (i-Start>2) 
                    {
                        Seq head = new Seq(Arr, Sums, Start, i-1);
                        result.Add(head);
                        result.AddRange(head.Split());
                    }
                    if (End-i>2)
                    {
                        Seq tail = new Seq(Arr, Sums, i+1,End);   
                        result.Add(tail);
                        result.AddRange(tail.Split());
                    }
                    
                }
            }
            return result;
        }
    }
    
    public int solution(int[] A) 
    {
        int[] pSums = new int[A.Length+1];
        
        for(int i = 0; i < A.Length; i ++)
        {
            pSums[i+1] = pSums[i] + A[i];     
        }
        
        double avg = (double)pSums[A.Length] / A.Length;
        Seq whole = new Seq(A, pSums, 0, A.Length-1);
        //Console.WriteLine("avg: "+avg+", "+whole.Avg);
        
        Seq min = whole;
        foreach(var eachSeq in whole.Split())
        {
            if (eachSeq.Avg < min.Avg)
            {
                min = eachSeq;   
            }
        }
        
        return min.Start;
    }
    
    
}
