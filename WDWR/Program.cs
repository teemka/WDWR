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
            double[] cena = { 170, 170, 170 };
            double[] cenaMag = { 10, 10, 10 };
            double[] twardosc = { 8.4, 6.2, 2.0 };
            double[] twardosc3 = { 3,3,3 };
            double[] twardosc6 = { 6,6,6 };
            double[][] kosztOleju = new double[2][];
            kosztOleju[0] = new double[3] { 116, 102, 113 }; 
            kosztOleju[1] = new double[3] { 100, 107, 110 }; // TEMP

            INumVar[][] olejMag = new INumVar[3][];
            INumVar[][] olejKup = new INumVar[2][];
            INumVar[][] olejRaf = new INumVar[2][]; // [msc][A-C]
            Cplex cplex = new Cplex();
            for(int i=0;i<3;i++)
            {
                olejMag[i] = new INumVar[3];
                for (int j = 0; j < 3; j++)
                {
                    olejMag[i][j] = cplex.NumVar(0, 800); // Ograniczenia na pojemność magazynu                    
                }                
            }
            for (int i = 0; i < 2; i++)
            {
                olejKup[i] = new INumVar[3];
                olejRaf[i] = new INumVar[3];
                for (int j = 0; j < 3; j++) // Ograniczenia na możliwości rafinacji
                {
                    if (j != 2)
                    {
                        olejRaf[i][j] = cplex.NumVar(0, 220);
                    }
                    else
                    {
                        olejRaf[i][j] = cplex.NumVar(0, 270);                       
                    }
                    olejKup[i][j] = cplex.NumVar(0, 1000);
                }
            }
            ILinearNumExpr mean = cplex.LinearNumExpr();
            for (int i = 0; i < 2; i++)
            {
                cplex.Ge(cplex.ScalProd(twardosc3, olejRaf[i]), cplex.ScalProd(twardosc, olejRaf[i])); // Ograniczenie na twardosc 
                cplex.Le(cplex.ScalProd(twardosc, olejRaf[i]),cplex.ScalProd(twardosc6, olejRaf[i]));
            }
            for (int i = 0; i < 3; i++)
            {
                cplex.AddEq(olejMag[0][i], 200.0); // Ograniczenie na stan magazynu w grudniu
                cplex.AddEq(olejMag[2][i], 200.0); // Ograniczenie na stan magazynu w lutym
                cplex.AddEq(olejMag[1][i], cplex.Sum(cplex.Diff(olejKup[0][i], olejRaf[0][i]),olejMag[0][i]));
                cplex.AddEq(olejMag[2][i], cplex.Sum(cplex.Diff(olejKup[1][i], olejRaf[1][i]), olejMag[1][i]));
            }

            cplex.AddMaximize(cplex.Diff(cplex.Diff(cplex.Sum(cplex.ScalProd(cena,olejRaf[0]), cplex.ScalProd(cena,olejRaf[1])),cplex.Sum(cplex.ScalProd(cenaMag,olejMag[0]), cplex.ScalProd(cenaMag,olejMag[1]), cplex.ScalProd(cenaMag, olejMag[2]))),
                cplex.Sum(cplex.ScalProd(kosztOleju[0], olejRaf[0]), cplex.ScalProd(kosztOleju[1], olejRaf[1]))));

            Console.WriteLine("licze");
            if (cplex.Solve())
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Solution status = " + cplex.GetStatus());
                System.Console.WriteLine();
                System.Console.WriteLine(" cost = " + cplex.ObjValue);
                for (int j = 0; j < 2; j++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        System.Console.WriteLine(" olejRaf[" + j + "][" + i + "] = " + cplex.GetValue(olejRaf[j][i]));
                    }
                }
                Console.WriteLine();
                for (int j = 0; j < 2; j++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        System.Console.WriteLine(" olejKup[" + j + "][" + i + "] = " + cplex.GetValue(olejKup[j][i]));
                    }
                }
                Console.WriteLine();
                for (int j = 0; j < 3; j++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        System.Console.WriteLine(" olejMag[" + j + "][" + i + "] = " + cplex.GetValue(olejMag[j][i]));
                    }
                }

                System.Console.WriteLine();
            }
            Console.ReadKey();
        }
        static double[][] GenerujWektorLosowy()
        {
            double[] wektorWartosciOczekiwanych = { 116, 102, 113, 100, 107, 110 };
            double[] diagonalnaMacierzyKowariancji = { 1, 36, 4, 49, 16, 9 };
            double[][] kosztOleju = new double[2][];
            for (int i = 0; i < 2; i++)
            {
                kosztOleju[i] = new double[3];
                for (int j = 0; j < 3; j++)
                {
                    do
                    {
                        kosztOleju[i][j] = StudentT.Sample(wektorWartosciOczekiwanych[i + j], Math.Sqrt(diagonalnaMacierzyKowariancji[i + j]), 4);
                    }
                    while (80 <= kosztOleju[i][j] && kosztOleju[i][j] <= 120);
                }
            }
            return null;
        }
    }
}
