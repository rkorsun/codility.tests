using System;
using System.Collections.Generic;


class Solution 
{
    public int solution(int[] H) 
    {
        Stack<int> placed = new Stack<int>();
        
        int result = 0;        
        int currHeight = 0;
        for(int i=0; i<H.Length; i++)
        {
            int nextHeight = H[i];
        
            while (currHeight > nextHeight)
            {
                int lastBrickHeight = placed.Pop();           
                currHeight -= lastBrickHeight;
            }
            
            if (currHeight < nextHeight) 
            {
                int nextBrickHeight = nextHeight-currHeight;        
                placed.Push(nextBrickHeight);
                result++;
                currHeight = nextHeight;
            }
        }
        
        return result;
    }
}
