using System.Collections.Generic;

namespace ATMcalculator.Logic
{
    public struct ChangeResult
    {
        public IList<KeyValuePair<int, int>> Denominations { get; set; }
        public int Remaining { get; set; }
    }
}
