using System;
// you can also use other imports, for example:
 using System.Collections.Generic;


class Solution {
        
    public int solution(int[] A) 
    {
        double min = (double)(A[0]+A[1]) / 2;
        int pos = 0;
        for(int i = 0; i < A.Length-1; i ++)
        {
            double avg2 = (double)(A[i]+A[i+1]) / 2;
            if (avg2 < min)
            {
                min = avg2;   
                pos = i;
            }
            if (i<A.Length-2)
            { 
                double avg3 = (double)(A[i] + A[i+1] + A[i+2]) / 3;
                if (avg3 < min)
                {
                    min = avg3;   
                    pos = i;
                }
            }
        }
        return pos;
    }
    
    
}
