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
            double sum2 = A[i]+A[i+1];
            double avg2 = sum2 / 2;
            if (avg2 < min)
            {
                min = avg2;   
                pos = i;
            }
            if (i<A.Length-2)
            { 
                double avg3 = (sum2 + A[i+2]) / 3;
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
