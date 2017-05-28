using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.Distributions;
using ILOG.Concert;
using ILOG.CPLEX;

namespace WDWR
{
    class Program
    {
        static void Main(string[] args)
        {
            double[] price = { 170.0, 170.0, 170.0 };
            double[] priceStorage = { 10.0, 10.0, 10.0 };
            double[] hardness = { 8.4, 6.2, 2.0 };
            double[] hardness3 = { 3,3,3 };
            double[] hardness6 = { 6,6,6 };
            double[][] oilCost = new double[2][];
            oilCost[0] = new double[3] { 116.0, 102.0, 113.0 }; 
            oilCost[1] = new double[3] { 100.0, 107.0, 110.0 }; // TEMP

            oilCost = GenerateRandomVector();

            INumVar[][] oilStore = new INumVar[3][];
            INumVar[][] oilBuy = new INumVar[2][];
            INumVar[][] oilProduce = new INumVar[2][]; // [month][A-C]
            Cplex cplex = new Cplex();
            for(int i=0;i<3;i++)
            {
                oilStore[i] = new INumVar[3];
                for (int j = 0; j < 3; j++)
                {
                    oilStore[i][j] = cplex.NumVar(0, 800); // Ograniczenia na pojemność magazynu                    
                }                
            }
            for (int i = 0; i < 2; i++)
            {
                oilBuy[i] = new INumVar[3];
                oilProduce[i] = new INumVar[3];
                for (int j = 0; j < 3; j++) // Ograniczenia na możliwości rafinacji
                {
                    if (j != 2)
                    {
                        oilProduce[i][j] = cplex.NumVar(0, 220);
                    }
                    else
                    {
                        oilProduce[i][j] = cplex.NumVar(0, 270);                       
                    }
                    oilBuy[i][j] = cplex.NumVar(0, 1070);
                }
            }
            ILinearNumExpr mean = cplex.LinearNumExpr();
            for (int i = 0; i < 2; i++)
            {
                cplex.AddGe(cplex.ScalProd(hardness, oilProduce[i]), cplex.ScalProd(hardness3, oilProduce[i])); // Hardness 
                cplex.AddLe(cplex.ScalProd(hardness, oilProduce[i]), cplex.ScalProd(hardness6, oilProduce[i]));
            }
            for (int i = 0; i < 3; i++)
            {
                cplex.AddEq(oilStore[0][i], 200.0); // Ograniczenie na stan magazynu w grudniu                
                cplex.AddEq(oilStore[2][i], 200.0); // Ograniczenie na stan magazynu w lutym
                //cplex.AddGe( oilProduce[0][i], cplex.Sum(oilStore[0][i], oilBuy[0][i]));
                //cplex.AddGe(oilProduce[1][i], cplex.Sum(oilStore[1][i], oilBuy[1][i]));
                cplex.AddEq(oilStore[1][i], cplex.Sum(cplex.Diff(oilBuy[0][i], oilProduce[0][i]), oilStore[0][i]));
                cplex.AddEq(oilStore[2][i], cplex.Sum(cplex.Diff(oilBuy[1][i], oilProduce[1][i]), oilStore[1][i]));
                //cplex.Eq(cplex.Sum(oilBuy[0][i],oilBuy[1][i],oilStore[0][i]),cplex.Sum(oilProduce[0][i], oilProduce[1][i]));
            }

            cplex.AddMaximize(cplex.Diff
                (cplex.Diff(cplex.Sum(cplex.ScalProd(price, oilProduce[0]), cplex.ScalProd(price, oilProduce[1])), cplex.Sum(cplex.ScalProd(priceStorage, oilStore[1]), cplex.ScalProd(priceStorage, oilStore[2]))),
                cplex.Sum(cplex.ScalProd(oilCost[0], oilBuy[0]), cplex.ScalProd(oilCost[1], oilBuy[1]))));


            if (cplex.Solve())
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Solution status = " + cplex.GetStatus());
                System.Console.WriteLine();
                System.Console.WriteLine("  = " + cplex.ObjValue);
                Console.WriteLine();
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Console.WriteLine(" oilCost[" + i + "][" + j + "] = " + oilCost[i][j]);
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
                        hardnessTotal += cplex.GetValue(oilProduce[j][i]) *hardness[i];
                        sum += cplex.GetValue(oilProduce[j][i]);
                    }
                    System.Console.WriteLine(" hardnessTotal[" + j + "] = " + hardnessTotal/sum);
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

                System.Console.WriteLine();
            }
            Console.ReadKey();
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
                    do
                    {
                        oilCost[i][j] = StudentT.Sample(excpectationVector[i + j], Math.Sqrt(variance[i + j]), 4);
                    }
                    while (80 >= oilCost[i][j] && oilCost[i][j] >= 120);                    
                }
            }
            return oilCost;
        }
    }
}
