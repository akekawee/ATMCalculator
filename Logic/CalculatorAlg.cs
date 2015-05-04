using System;
using System.Collections.Generic;
using System.Linq;

namespace ATMcalculator.Logic
{
    public class CalculatorAlg
    {
        /// <summary>
        /// ATM calculator Logic support all currency, support note and coin dispenser 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="denoms"></param>
        /// <param name="limits"></param>
        /// <returns></returns>
        public ChangeResult FindOptimalChange(int target, int[] denoms, int[] limits = null)
        {
            var denomList = GetDenominationData(denoms, limits);
            CalculatorLogic(ref target, denomList, true);// Calculate note 
            IList<DenominationsModel> results = CalculatorLogic(ref target, denomList, false);// Calculate coin 
            if (target < 0)
                throw new ArgumentException("An error has occurred - Please Contact Your Administrator");
            return new ChangeResult { Denominations = results.Select(c => new KeyValuePair<int, int>(c.Denom, c.Count)).ToList(), Remaining = target };
        }

        private IList<DenominationsModel> CalculatorLogic(ref int target, IList<DenominationsModel> denomList, bool isNote)
        {
            IList<IList<DenominationsModel>> results = new List<IList<DenominationsModel>>();
            // Check Denominations if least than 500($5) mean note 
            IList<DenominationsModel> targetList = isNote ? denomList.Where(c => c.Denom >= 500 && c.Denom > 0).ToList() : denomList.Where(c => c.Denom < 500 && c.Denom > 0).ToList();
            //
            if (targetList.Count == 0)
                return denomList;
            int requestTarget = 0;
            for (int i = target; i > 0; i--)
                if (i % targetList.Min(c => c.Denom) == 0)
                {
                    requestTarget = i;
                    break;
                }
            if (requestTarget == 0)
                return denomList;
            int lastRequestTarget = requestTarget;
            target -= requestTarget; // target left before calculate note
            requestTarget = FirstCompute(targetList, requestTarget, results); // First Calculate
            int noteTargetLeftFirstCal = requestTarget; // note target left after first calculate note
            if (results.Count == 0)
            {
                target += requestTarget;
                return denomList;
            }
            List<DenominationsModel> backup = results[0].Select(r => new DenominationsModel
            {
                Row = r.Row,
                Denom = r.Denom,
                Limit = r.Limit,
                Count = r.Count
            }).ToList();

            if (requestTarget == 0)
            {
                foreach (var t in results[0])
                {
                    denomList.Where(c => c.Row == t.Row).ToList().ForEach(c =>
                    {
                        c.Count = t.Count;
                        c.Limit -= c.Count;
                    });
                }
            }
            else
            {
                if (results[0].Sum(c => c.Limit) == 0)
                {
                    foreach (var t in results[0])
                    {
                        denomList.Where(c => c.Row == t.Row).ToList().ForEach(c =>
                        {
                            c.Count = t.Count;
                            c.Limit -= c.Count;
                        });
                    }
                    target += (lastRequestTarget - results[0].Sum(c => c.Denom * c.Count));
                    return denomList;
                }
                if (requestTarget != 0 && results[0].Count > 0)
                {
                    results.Add(results[0]); // Copy to the new list to second note calculate
                    var higherCount = results[1].FirstOrDefault().Count;
                    var higherDenom = results[1].FirstOrDefault().Denom;
                    var higherRowId = results[1].FirstOrDefault().Row;

                    for (int i = 1; i <= higherCount; i++)
                    {
                        if (requestTarget > 0 && results[1].Where(c => c.Row == higherRowId).Select(c => c.Count).FirstOrDefault() > i)
                        {
                            if (((higherDenom * i) + requestTarget) > targetList.Where(c => c.Limit > 0).Min(c => c.Denom))
                            {
                                foreach (var list in targetList.OrderByDescending(c => c.Denom).Where(c => c.Limit > 0 && c.Denom != higherDenom))
                                {
                                    if (results[1].Count(c => c.Row == list.Row) != 0)
                                    {
                                        var value = results[1].Where(c => c.Row == list.Row).Select(c => c.Count * c.Denom).FirstOrDefault();
                                        var index = results[1].ToList().FindIndex(c => c.Row == list.Row);
                                        results[1].RemoveAt(index);
                                        requestTarget += value;
                                    }
                                    if (((higherDenom * i) + requestTarget) % list.Denom < targetList.Where(c => c.Limit > 0).Min(c => c.Denom))
                                    {
                                        requestTarget += (higherDenom * i);
                                        results[1].Where(c => c.Row == higherRowId).ToList().ForEach(c => { c.Count -= i; });
                                        foreach (var l in targetList.Where(c => c.Limit > 0 && c.Denom != higherDenom).OrderByDescending(c => c.Denom))
                                        {
                                            if (requestTarget < l.Denom)
                                                break;
                                            for (int j = 0; j < l.Limit; j++)
                                            {
                                                requestTarget -= l.Denom;
                                                if (requestTarget < l.Denom)
                                                {
                                                    if (results[1].Count(c => c.Denom == l.Denom) != 0)
                                                        results[1].Where(c => c.Row == l.Row).ToList().ForEach(c =>
                                                        {
                                                            c.Limit = c.Limit - (j + 1);
                                                            c.Count = c.Count + (j + 1);
                                                        });
                                                    else
                                                        results[1].Add(new DenominationsModel
                                                        {
                                                            Row = l.Row,
                                                            Denom = l.Denom,
                                                            Limit = l.Limit - (j + 1),
                                                            Count = (j + 1)
                                                        });
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    if (requestTarget < targetList.Where(c => c.Limit > 0).Min(c => c.Denom))
                                        break;
                                }
                            }
                            else
                                break;
                        }
                        else
                        {
                            requestTarget = lastRequestTarget;
                            results[1].Clear();
                            foreach (var list in targetList.Where(c => c.Limit > 0 && c.Denom != higherDenom).OrderByDescending(c => c.Denom))
                            {
                                if (requestTarget < list.Denom)
                                    break;
                                for (int j = 0; j < list.Limit; j++)
                                {
                                    requestTarget -= list.Denom;
                                    if (requestTarget < list.Denom)
                                    {
                                        results[1].Add(new DenominationsModel
                                        {
                                            Row = list.Row,
                                            Denom = list.Denom,
                                            Limit = list.Limit - (j + 1),
                                            Count = (j + 1)
                                        });
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                        if (requestTarget < targetList.Where(c => c.Limit > 0).Min(c => c.Denom))
                            break;
                        if (requestTarget == 0)
                            break;
                    }
                    var jj = results[0].Sum(c => c.Denom * c.Count);
                    var ii = backup.Sum(c => c.Denom * c.Count);

                    if (lastRequestTarget - jj >= lastRequestTarget - ii) // Check result between backup and list
                    {
                        results[0] = backup;
                        foreach (var t in results[0])
                        {
                            denomList.Where(c => c.Row == t.Row).ToList().ForEach(c =>
                            {
                                c.Count = t.Count;
                                c.Limit -= c.Count;
                            });
                        }
                        target += (lastRequestTarget - results[0].Sum(c => c.Denom * c.Count));
                        return denomList;
                    }
                    foreach (var t in results[1])
                    {
                        denomList.Where(c => c.Row == t.Row).ToList().ForEach(c =>
                        {
                            c.Count = t.Count;
                            c.Limit -= c.Count;
                        });
                    }
                    target += (lastRequestTarget - results[0].Sum(c => c.Denom * c.Count));
                    return denomList;
                }

                if (requestTarget > noteTargetLeftFirstCal && requestTarget != 0)
                {
                    foreach (var t in results[0])
                    {
                        denomList.Where(c => c.Row == t.Row).ToList().ForEach(c =>
                        {
                            c.Count = t.Count;
                            c.Limit -= c.Count;
                        });
                    }
                    target += noteTargetLeftFirstCal;
                    return denomList;
                }
            }
            return denomList;
        }

        private int FirstCompute(IEnumerable<DenominationsModel> targetList, int requestTarget, IList<IList<DenominationsModel>> results)
        {
            foreach (var list in targetList.OrderByDescending(c => c.Denom).Where(c => c.Limit > 0))
            {
                if (requestTarget >= list.Denom)
                {
                    int count = requestTarget / list.Denom;
                    if (list.Limit >= count)
                    {
                        if (results.Count == 0)
                            results.Add(new List<DenominationsModel>
                            {
                                new DenominationsModel
                                {
                                    Row = list.Row,
                                    Denom = list.Denom,
                                    Limit = list.Limit - count,
                                    Count = count
                                }
                            });
                        else
                            results[0].Add(new DenominationsModel
                            {
                                Row = list.Row,
                                Denom = list.Denom,
                                Limit = list.Limit - count,
                                Count = count
                            });
                        requestTarget -= count * list.Denom;
                    }
                    else
                    {
                        if (results.Count == 0)
                            results.Add(new List<DenominationsModel>
                            {
                                new DenominationsModel
                                {
                                    Row = list.Row,
                                    Denom = list.Denom,
                                    Limit = list.Limit - list.Limit,
                                    Count = list.Limit
                                }
                            });
                        else
                            results[0].Add(new DenominationsModel
                            {
                                Row = list.Row,
                                Denom = list.Denom,
                                Limit = list.Limit - list.Limit,
                                Count = list.Limit
                            });
                        requestTarget -= list.Limit * list.Denom;
                    }
                }
            }
            return requestTarget;
        }

        private IList<DenominationsModel> GetDenominationData(int[] denoms, int[] limits)
        {
            if (denoms == null)
                throw new ArgumentNullException("denoms");
            if (limits != null && denoms.Length != limits.Length)
                throw new ArgumentException("denoms and limits must have same no of items");
            if (limits == null)
            {
                limits = new int[denoms.Length];
                for (int i = 0; i < limits.Length; i++)
                    limits[i] = int.MaxValue;
            }
            return denoms.Select((t, i) => new DenominationsModel { Row = i, Denom = t, Limit = limits[i], Count = 0 }).ToList();
        }

        internal class DenominationsModel
        {
            public int Row { get; set; }
            public int Denom { get; set; }
            public int Limit { get; set; }
            public int Count { get; set; }
        }

    }
}
