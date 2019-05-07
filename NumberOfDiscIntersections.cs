using System;
// you can also use other imports, for example:
// using System.Collections.Generic;

// you can write to stdout for debugging purposes, e.g.
// Console.WriteLine("this is a debug message");

class Solution 
{
    
    public int solution(int[] A) 
    {
        int len = A.Length;
        int[] starts = new int[len]; 
        int[] ends = new int[len]; 
        
        
        for(int i=0; i<len; i++)
        {
            int eachR = A[i];
            starts[i] = i-eachR;
            ends[i] = i+eachR;
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
            
        }
        
        return intersections; 
    }
}
