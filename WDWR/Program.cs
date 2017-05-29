using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using ILOG.Concert;
using ILOG.CPLEX;
using System.Threading.Tasks;

namespace WDWR
{
    class Program
    {
        static void Main(string[] args)
        {
            int scenarioCount = 10;
            double srednia = 55155.51574465;
            double[] price = { 170.0, 170.0, 170.0 };
            double[] priceStorage = { 10.0, 10.0, 10.0 };
            double[] hardness = { 8.4, 6.2, 2.0 };
            double[] hardness3 = { 3, 3, 3 };
            double[] hardness6 = { 6, 6, 6 };
            double[][] oilCostTotal = new double[2][];
            double[] profit = new double[scenarioCount];
            double[] risk = new double[scenarioCount];
            List<double[][]> oilCostList = new List<double[][]>();
            double[][] oilCost = new double[2][];
            oilCost[0] = new double[3] { 1.159550009798596e+02, 1.018115866059220e+02, 1.128780935294160e+02 };
            oilCost[1] = new double[3] { 100, 1.067537671189185e+02, 1.098041326934309e+02 };

            for (int i = 0; i < scenarioCount; i++)
            {
                oilCostList.Add(GenerateRandomVector());
                //oilCostList.Add(oilCost);
            }
            System.IO.StreamWriter zad2 = new System.IO.StreamWriter(@"C:\Users\tomas\Google Drive\school\EiTI\WDWR\zad2.1.csv");
            System.IO.StreamWriter vector = new System.IO.StreamWriter(@"C:\Users\tomas\Google Drive\school\EiTI\WDWR\wektor.1.csv");
            System.IO.StreamWriter FSD = new System.IO.StreamWriter(@"C:\Users\tomas\Google Drive\school\EiTI\WDWR\FSD.1.csv");
            zad2.WriteLine("Average Profit;Risk;limes");

            for(int iter = 0; iter < 100; iter++)
            {
                double lim = iter * 600;
                INumVar[][] oilStore = new INumVar[3][];
                INumVar[][] oilBuy = new INumVar[2][];
                INumVar[][] oilProduce = new INumVar[2][]; // [month][A-C]
                Cplex cplex = new Cplex();
                for (int i = 0; i < 2; i++) // Initialize oilBuy and oilProduce
                {
                    oilCostTotal[i] = new double[3];
                    oilBuy[i] = new INumVar[3];
                    oilProduce[i] = new INumVar[3];
                }
                for (int i = 0; i < 3; i++) // Initialize oilStore
                {
                    oilStore[i] = new INumVar[3];
                    for (int j = 0; j < 3; j++)
                    {
                        oilStore[i][j] = cplex.NumVar(0, 800); // Ograniczenia na pojemność magazynu                    
                    }
                }
                for (int i = 0; i < 2; i++) // Ograniczenia na miesiące
                {
                    for (int j = 0; j < 3; j++) // Ograniczenia na możliwości rafinacji
                    {
                        if (j != 2)
                        {
                            oilProduce[i][j] = cplex.NumVar(0, 220); // Rafinacja roślinnego
                        }
                        else
                        {
                            oilProduce[i][j] = cplex.NumVar(0, 270); // Rafinacja nie-roślinnego                
                        }
                        oilBuy[i][j] = cplex.NumVar(0, 1070);
                    }
                    cplex.AddRange(0, cplex.Sum(oilProduce[i][0], oilProduce[i][1]), 220);
                    cplex.AddGe(cplex.ScalProd(hardness, oilProduce[i]), cplex.ScalProd(hardness3, oilProduce[i])); // Hardness greater than 3
                    cplex.AddLe(cplex.ScalProd(hardness, oilProduce[i]), cplex.ScalProd(hardness6, oilProduce[i])); // Hardness less than 6
                }
                for (int i = 0; i < 3; i++) // Ograniczenia na oleje
                {
                    cplex.AddEq(oilStore[0][i], 200.0); // Ograniczenie na stan magazynu w grudniu                
                    cplex.AddEq(oilStore[2][i], 200.0); // Ograniczenie na stan magazynu w lutym                
                    cplex.AddEq(oilStore[1][i], cplex.Sum(cplex.Diff(oilBuy[0][i], oilProduce[0][i]), oilStore[0][i])); // (Kupowane + zmagazynowane - produkowane) w tym miesiacu = zmagazynowane w nastepnym miesiacu
                    cplex.AddEq(oilStore[2][i], cplex.Sum(cplex.Diff(oilBuy[1][i], oilProduce[1][i]), oilStore[1][i]));
                }
                
                INumExpr[] arrayOfEq = new INumExpr[oilCostList.Count];
                for (int i = 0; i < oilCostList.Count; i++)
                {
                    ILinearNumExpr Revenue = cplex.LinearNumExpr();
                    ILinearNumExpr StorageCost = cplex.LinearNumExpr();
                    ILinearNumExpr BuyCost = cplex.LinearNumExpr();
                    for (int j = 0; j < 2; j++)
                    {
                        for (int k = 0; k < 3; k++)
                            oilCostTotal[j][k] += oilCostList[i][j][k];
                        Revenue.AddTerms(price, oilProduce[j]);
                        StorageCost.AddTerms(priceStorage, oilStore[j + 1]);
                        BuyCost.AddTerms(oilCostList[0][j], oilBuy[j]);
                    }
                    cplex.AddLe(cplex.Diff(Revenue, cplex.Sum(BuyCost, StorageCost)), lim);
                    arrayOfEq[i] = cplex.Diff(srednia, cplex.Diff(Revenue, cplex.Sum(BuyCost, StorageCost)));
                }
                // Funkcja Celu: zyski ze sprzedaży - koszta magazynowania - koszta kupowania materiału do produkcji
                //cplex.AddMaximize(cplex.Diff(Revenue,cplex.Sum(BuyCost,StorageCost)));
                // Funkcja Celu: minimazlicacja odchylenia maksymalnego ryzyka
                cplex.AddMinimize(cplex.Abs(cplex.Max(arrayOfEq)));

                if (cplex.Solve())
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("Solution status = " + cplex.GetStatus());
                    System.Console.WriteLine();
                    System.Console.WriteLine(" Fn Celu = " + cplex.ObjValue);
                    Console.WriteLine();
                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            Console.WriteLine(" oilCostAverage[" + i + "][" + j + "] = " + oilCostTotal[i][j] / oilCostList.Count);
                        }
                    }
                    Console.WriteLine();
                    for (int j = 0; j < 2; j++)
                    {
                        double hardnessTotal = 0;
                        double sum = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            System.Console.WriteLine(" oilProduce[" + j + "][" + i + "] = " + cplex.GetValue(oilProduce[j][i]));
                            hardnessTotal += cplex.GetValue(oilProduce[j][i]) * hardness[i];
                            sum += cplex.GetValue(oilProduce[j][i]);
                        }
                        System.Console.WriteLine(" hardnessTotal[" + j + "] = " + hardnessTotal / sum);
                        Console.WriteLine();
                    }
                    for (int j = 0; j < 2; j++)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            System.Console.WriteLine(" oilBuy[" + j + "][" + i + "] = " + cplex.GetValue(oilBuy[j][i]));
                        }
                    }
                    Console.WriteLine();
                    for (int j = 0; j < 3; j++)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            System.Console.WriteLine(" oilStore[" + j + "][" + i + "] = " + cplex.GetValue(oilStore[j][i]));
                        }
                    }
                    Console.WriteLine();
                    for (int i = 0; i < oilCostList.Count; i++)
                    {
                        double revenue = 0;
                        double storageCost = 0;
                        double buyCost = 0;
                        for (int j = 0; j < 2; j++)
                        {
                            for (int k = 0; k < 3; k++)
                            {
                                revenue += cplex.GetValue(oilProduce[j][k]) * 170;
                                storageCost += cplex.GetValue(oilStore[j][k]) * 10;
                                buyCost += cplex.GetValue(oilBuy[j][k]) * oilCostList[i][j][k];
                                if(iter==0)
                                    vector.Write(oilCostList[i][j][k] + ";");
                            }
                        }
                        if (iter == 0)
                            vector.WriteLine();
                        profit[i] = revenue - storageCost - buyCost;
                    }
                    double riskMax = 0;
                    var avg = profit.Average();
                    if (iter == 25 || iter == 50 || iter == 75)
                        FSD.WriteLine(iter + ";risk");
                    for (int i = 0; i<oilCostList.Count; i++)
                    {
                        var diff = Math.Abs(profit[i] - avg);
                        risk[i] = diff;
                        if(iter == 25 || iter==50 || iter==75)
                            FSD.WriteLine(profit[i] + ";" + risk[i]);
                        if (diff > riskMax)
                            riskMax = diff;
                    }                    
                    Console.WriteLine(" Average Profit =" + avg);
                    Console.WriteLine(" Risk =" + riskMax);
                    Console.WriteLine(" iter =" + iter);
                    Console.WriteLine();                  
                    
                    zad2.WriteLine(avg + ";" + riskMax + ";" + lim);
                }
            }
            zad2.Close();
            vector.Close();
            FSD.Close();
        }
        static double[][] GenerateRandomVector()
        {
            double[] excpectationVector = { 116, 102, 113, 100, 107, 110 };
            double[] variance = { 1, 36, 4, 49, 16, 9 };
            double[][] oilCost = new double[2][];
            for (int i = 0; i < 2; i++)
            {
                oilCost[i] = new double[3];
                for (int j = 0; j < 3; j++)
                {
                    //do
                    //{
                    //    oilCost[i][j] = StudentT.Sample(excpectationVector[i*3 + j], Math.Sqrt(variance[i*3 + j]), 4);
                    //}
                    //while (80 >= oilCost[i][j] && oilCost[i][j] >= 120);               
                    oilCost[i][j] = StudentT.Sample(excpectationVector[i * 3 + j], Math.Sqrt(variance[i * 3 + j]), 4);
                    if (oilCost[i][j] < 80) oilCost[i][j] = 80;
                    if (oilCost[i][j] > 120) oilCost[i][j] = 120;
                }
            }
            return oilCost;
        }           
    }
}
