using System;
// you can also use other imports, for example:
// using System.Collections.Generic;

// you can write to stdout for debugging purposes, e.g.
// Console.WriteLine("this is a debug message");

class Solution 
{
    private const int LIMIT = 10000000;
    
    public int solution(int[] A) 
    {
        int len = A.Length;
        long[] starts = new long[len]; 
        long[] ends = new long[len]; 
        
        
        for(int i=0; i<len; i++)
        {
            long eachR = A[i];
            starts[i] = (long)i-eachR;
            ends[i] = (long)i+eachR;
        }
        Array.Sort(starts);
        Array.Sort(ends);
        
        int currStart=0;
        int currEnd=0;
        int currOpenCount = 0;
        int intersections = 0;
        
        while (currStart < len && currEnd<len)
        {
            //Console.WriteLine($"start[{currStart}]=={starts[currStart]}.... ends[{currEnd}]={ends[currEnd]}");
            if (starts[currStart] <= ends[currEnd])
            {
                intersections += currOpenCount;
                currOpenCount++;
                currStart++;    
                //Console.WriteLine($"open... opened={currOpenCount}, intersection={intersections}");
            }
            else 
            {
                currOpenCount--;
                //Console.WriteLine($"close... opened={currOpenCount}, intersection={intersections}");
                currEnd++;   
            }
         
            if(intersections > LIMIT)
            {
                return -1;   
            }
        }
        
        return intersections; 
    }
}
