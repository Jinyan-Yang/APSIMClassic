﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Globalization;
using ModelFramework;
using CSGeneral;
using MathNet.Numerics.LinearAlgebra.Double;

public class SoilArbitrator
{
    [Link]
    Simulation paddock;
    [Input]
    DateTime Today;   // Equates to the value of the current simulation date - value comes from CLOCK
    [Output] // used to tell the maize model to not do the water balance
    double arbitrator = 1.0f;

    Component SoilWat;
    RootSystemType RootData;
    DataTable AllRootSystems;

    // Initialize IFormatProvider to print matrix/vector data
    CultureInfo formatProvider = (CultureInfo)CultureInfo.InvariantCulture.Clone();


    // The following event handler will be called once at the beginning of the simulation
    [EventHandler]
    public void OnInitialised()
    {
        AllRootSystems = new DataTable();
        AllRootSystems.Columns.Add("ZoneName", typeof(string));
        AllRootSystems.Columns.Add("CropType", typeof(string));
        AllRootSystems.Columns.Add("SWDemand", typeof(double));
        AllRootSystems.Columns.Add("ZoneStrength", typeof(Dictionary<string, double>)); //KVP of zones and relative strengths for water extraction
        AllRootSystems.Columns.Add("RootSystemZone", typeof(RootSystemZoneType));
        AllRootSystems.Columns.Add("HasRootSystem", typeof(bool));
        RootData = new RootSystemType();
        formatProvider.TextInfo.ListSeparator = " ";
    }

    [EventHandler]
    public void OnProcess()
    {
        //set up data table
        int NumLayers = 0;
        AllRootSystems.Rows.Clear();
        foreach (Paddock p in paddock.ChildPaddocks)
        {
            foreach (Component c in p.Crops)
            {
                string PlantStatus;
                if (!c.Get("plant_status", out PlantStatus))
                    throw new Exception("Could not find plant_status for crop :" + c.Name);

                if (PlantStatus != "out") //if crop is not in ground, we don't care about it
                {
                    if (c.GetObject("RootSystem", ref RootData)) //crop has a RootData structre
                    {
                        Dictionary<string, double> SWStrength = CalcSWSourceStrength(RootData);
                        foreach (RootSystemZoneType zone in RootData.Zone) //add each zone to the table
                            AllRootSystems.Rows.Add(zone.ZoneName, c.Name, RootData.SWDemand, SWStrength, zone, true);
                        NumLayers = RootData.Zone[0].kl.Length;
                    }
                    else //crop does not have RootData structure, so make one.
                    {
                        Dictionary<string, double> SWStrength = new Dictionary<string, double>();
                        RootData = new RootSystemType();
                        RootData.Zone = new RootSystemZoneType[1];
                        RootData.Zone[0] = new RootSystemZoneType();
                        RootData.Zone[0].ZoneName = p.Name;
                        RootData.Zone[0].ZoneArea = 1;
                        if (!c.Get("sw_demand", out RootData.SWDemand))
                            throw new Exception("Could not get sw_demand for crop " + c.Name);
                        if (!c.Get("root_depth", out RootData.Zone[0].RootDepth))
                            throw new Exception("Could not get root_depth for crop " + c.Name);
                        if (!c.Get("ll", out RootData.Zone[0].ll))
                            throw new Exception("Could not get ll for crop " + c.Name);
                        if (!c.Get("kl", out RootData.Zone[0].kl))
                            throw new Exception("Could not get kl for crop " + c.Name);
                        SoilWat = (Component)p.LinkByType("SoilWat");
                        SWStrength.Add(p.Name, 1);
                        if (!SoilWat.Get("dlayer", out RootData.Zone[0].dlayer))
                            throw new Exception("Could not get dlayer for paddock " + p.Name);
                        AllRootSystems.Rows.Add(RootData.Zone[0].ZoneName, c.Name, RootData.SWDemand, SWStrength, RootData.Zone[0], true);
                        NumLayers = RootData.Zone[0].kl.Length;
                    }
                }
            }
        }
        //use LINQ to extract the paddocks for processing
        IEnumerable<string> paddockNames = AllRootSystems.AsEnumerable().Select<DataRow, string>(name => (string)name.ItemArray[0]).Distinct();

        //do water allocation for each paddock
        foreach (string PaddockName in paddockNames)
        {
            IEnumerable<DataRow> RootZones = AllRootSystems.AsEnumerable().Where(row => row.ItemArray[0].Equals(PaddockName));
            Paddock p = (Paddock)paddock.LinkByName(PaddockName);
            Component fieldProps = (Component)p.LinkByName("FieldProps");
            double fieldArea;
            if (fieldProps == null || !fieldProps.Get("fieldArea", out fieldArea))
                throw new Exception("Could not find FieldProps component in field " + PaddockName);

            Component Soil = (Component)p.LinkByType("SoilWat");
            double[] SWDep;
            double[] dlayer;
            Soil.Get("dlayer", out dlayer);
            Soil.Get("sw_dep", out SWDep);
            double[] CropSWDemand = new double[RootZones.Count()];
            for (int i = 0; i < RootZones.Count(); i++) //get demand for all crops in paddock using relative SW strength
            {
                Dictionary<string, double> PaddockSWDemands = (Dictionary<string, double>)RootZones.ToArray()[i].ItemArray[3];
                CropSWDemand[i] = PaddockSWDemands[p.Name] * (double)RootZones.ToArray()[i].ItemArray[2];
            }
            double[,] RelKLStrength = CalcRelKLStrength(RootZones, CropSWDemand);
            double[,] RelSWLayerStrength = CalcRelSWLayerStrength(RootZones, SWDep, NumLayers);
            double[,] SWSupply = CalcSWSupply(RootZones, SWDep, NumLayers);

            double[,] LayerUptake = new double[RootZones.Count(), NumLayers];
            double[] LastCropSWDemand;
            double[,] LastSWSupply;

            int count = 0;
            do
            {
                count++;
                LastCropSWDemand = CropSWDemand;
                LastSWSupply = SWSupply;

                for (int i = 0; i < RootZones.Count(); i++) //get as much water as possible for the layer using relative kl strengths
                {
                    RootSystemZoneType Zone = (RootSystemZoneType)RootZones.ToArray()[i].ItemArray[4];
                    for (int j = 0; j < NumLayers; j++)
                    {
                        if (MathUtility.Sum(CropSWDemand) < MathUtility.Sum(SWSupply))
                        {
                            LayerUptake[i, j] = CropSWDemand[i] * RelSWLayerStrength[i, j];
                        }
                        else
                            LayerUptake[i, j] = SWSupply[i, j] * RelKLStrength[j, i] * RootProportion(j, Zone.RootDepth, dlayer);

                        if (LayerUptake[i, j] < 0)
                            throw new Exception("Layer uptake should not be negative"); 
                    }
                }

                DenseMatrix Uptake = DenseMatrix.OfArray(LayerUptake);
                Paddock CurrentPaddock;
                Component CurrentCrop;
                for (int i = 0; i < RootZones.Count(); i++) //subtract taken water from the supply and demand
                {
                    CurrentPaddock = (Paddock)p.LinkByName((string)RootZones.ToArray()[i].ItemArray[0]);
                    CurrentCrop = (Component)CurrentPaddock.LinkByName((string)RootZones.ToArray()[i].ItemArray[1]);
                    CropSWDemand[i] -= Uptake.Row(i).Sum();
                    if (CurrentCrop != null && CurrentCrop.Name.ToLower().Equals("maize"))
                    {
                        CurrentCrop.Set("arb_water_uptake", Uptake.Row(i).ToArray());
                    }
                    for (int j = 0; j < NumLayers; j++)
                    {
                        SWSupply[i, j] -= LayerUptake[i, j] ;
                    }
                }

                //subtract from soil water
                for (int j = 0; j < Uptake.ColumnCount; j++)
                {
                    SWDep[j] -= Uptake.Column(j).Sum() / fieldArea;
                }

                Soil.Set("sw_dep", SWDep);
            } while (MathUtility.Sum(LastCropSWDemand) != MathUtility.Sum(CropSWDemand) && MathUtility.Sum(LastSWSupply) != MathUtility.Sum(SWSupply));
        }
    }

    /// <summary>
    /// For each crop, calculate the maximum water uptake for each layer.
    /// </summary>
    /// <param name="CropSWDemand">An array holding the SW demand for each crop.</param>
    /// <param name="RelSWLayerStrength">The relative source strength for each crop and layer</param>
    /// <param name="NumLayers">Number of layers in the soil profile</param>
    /// <param name="RootZones">The rootzones to process in current paddock</param>
    /// <param name="dlayer">Layer depth array</param>
    /// <returns>A 2D array containing the maximum uptake for each crop and layer</returns>
    private double[,] CalcCropSWLayerUptake(double[] CropSWDemand, double[,] RelSWLayerStrength, int NumLayers, IEnumerable<DataRow> RootZones, double[] dlayer)
    {
        double[,] CropSWLayerUptake = new double[CropSWDemand.Length, NumLayers];
        for (int i = 0; i < CropSWDemand.Length; i++)
        {
            RootSystemZoneType Zone = (RootSystemZoneType)RootZones.ToArray()[i].ItemArray[4];
            for (int j = 0; j < NumLayers; j++)
            {
                CropSWLayerUptake[i, j] = CropSWDemand[i] * RelSWLayerStrength[i, j] * RootProportion(j, Zone.RootDepth, dlayer);
            }
        }

        return CropSWLayerUptake;
    }

    /// <summary>
    /// Calculate the amount of water available to each crop on a per layer basis.
    /// As crops will have different lower limits, they can have a different supply.
    /// </summary>
    /// <param name="RootZones">The rootzones to process in current paddock</param>
    /// <param name="SWDep">An array containing depth of soil water per layer (mm/mm)</param>
    /// <param name="NumLayers">Number of layers in the soil profile</param>
    /// <returns>A 2D array containing the SW supply available for each crop and layer.</returns>
    private double[,] CalcSWSupply(IEnumerable<DataRow> RootZones, double[] SWDep, int NumLayers)
    {
        RootSystemZoneType zone = new RootSystemZoneType();
        double[,] SWSupply = new double[RootZones.Count(), NumLayers];
        for (int i = 0; i < RootZones.Count(); i++) //crops
        {
            zone = (RootSystemZoneType)RootZones.ToArray()[i].ItemArray[4];
            for (int j = 0; j < NumLayers; j++)
            {
                SWSupply[i, j] = zone.kl[j] * (SWDep[j] - zone.ll[j] * zone.dlayer[j]) * zone.ZoneArea;
                if (SWSupply[i, j] < 0)
                    SWSupply[i, j] = 0; //can be < 0 if another crop with a lower LL has extracted below what this one can.
            }
        }
        return SWSupply;
    }


    /// <summary>
    /// Using the depth of soil water and current crop kl value, calculate a relative layer source strength for each crop.
    /// </summary>
    /// <param name="RootZones">The rootzones to process in current paddock</param>
    /// <param name="SWDep">An array containing depth of soil water per layer (mm/mm)</param>
    /// <param name="NumLayers">Number of layers in the soil profile</param>
    /// <returns>A 2D array containing the relative source strength for each crop and layer.</returns>
    private double[,] CalcRelSWLayerStrength(IEnumerable<DataRow> RootZones, double[] SWDep, int NumLayers)
    {
        RootSystemZoneType zone = new RootSystemZoneType();
        double[,] RelSWLayerStrength = new double[RootZones.Count(), NumLayers];
        for (int i = 0; i < RootZones.Count(); i++) //crops
        {
            double TotalSource = 0;
            zone = (RootSystemZoneType)RootZones.ToArray()[i].ItemArray[4];
            int DeepestRoot = CalcMaxRootLayer(zone.RootDepth, zone.dlayer);
            for (int j = 0; j < NumLayers; j++)
                if (j <= DeepestRoot)
                    TotalSource += zone.kl[j] * SWDep[j];
            for (int j = 0; j < NumLayers; j++)
                if (j <= DeepestRoot && zone.kl[j] > 0)
                    RelSWLayerStrength[i, j] = zone.kl[j] * SWDep[j] / TotalSource;
                else
                    RelSWLayerStrength[i, j] = 0;
        }
        return RelSWLayerStrength;
    }

    /// <summary>
    /// Calculate relative strength of crop kl for each layer.
    /// Will be 1 for a single crop.
    /// </summary>
    /// <param name="RootZones">The rootzones to process in current paddock</param>
    /// <returns>A 2D array containing the relative kl strength of each crop and layer.</returns>
    private double[,] CalcRelKLStrength(IEnumerable<DataRow> RootZones, double[] CropSWDemand)
    {
        double[][] KLArray = new double[RootZones.Count()][];
        int[] LowestRootLayer = new int[RootZones.Count()];
        for (int i = 0; i < KLArray.GetLength(0); i++) //extract the kl array from each zone
        {
            RootSystemZoneType zone = (RootSystemZoneType)RootZones.ToArray()[i].ItemArray[4];
            KLArray[i] = zone.kl;
            for (int j = 0; j < KLArray[i].Length; j++)
                if (CropSWDemand[i] == 0)
                    KLArray[i][j] = 0;
            LowestRootLayer[i] = CalcMaxRootLayer(zone.RootDepth, zone.dlayer);
        }

        //calculate relative demand strength for each layer
        double[,] RelKLStrength = new double[KLArray[0].Length, KLArray.Length];
        for (int i = 0; i < KLArray[0].Length; i++) //layer
        {
            double KLSum = 0;
            for (int j = 0; j < KLArray.Length; j++) //for the current layer, sum the kl's of each crop in the layer
            {
                if(i <= LowestRootLayer[j])
                    KLSum += KLArray[j][i];
            }
            for (int j = 0; j < KLArray.Length; j++) //use those summed kl's to calculate the relative kl strength for each crop in the layer
            {
                if (i <= LowestRootLayer[j] && KLArray[j][i] > 0)
                    RelKLStrength[i, j] = KLArray[j][i] / KLSum;
                else
                    RelKLStrength[i, j] = 0;
            }
        }
        return RelKLStrength;
    }

    /// <summary>
    /// Calculate the best paddocks to take water from when a crop is in multiple root zones.
    /// This method calculates the relative source strength of each paddock.
    /// </summary>
    /// <param name="RootData">A RootData structure provided by a crop.</param>
    /// <returns>A Dictionary containing the paddock names and relative strengths</returns>
    private Dictionary<string, double> CalcSWSourceStrength(RootSystemType RootData)
    {
        Dictionary<string, double> SoilWaters = new Dictionary<string, double>();
        string[] ZoneNames = new string[RootData.Zone.Length];
        double[] SWDeps = new double[RootData.Zone.Length];
        double TotalSW;
        Paddock p;
        Component Soil;

        for (int i = 0; i < ZoneNames.Length; i++)
        {
            double[] SWlayers;
            ZoneNames[i] = RootData.Zone[i].ZoneName;
            p = (Paddock)paddock.LinkByName(ZoneNames[i]);
            Soil = (Component)p.LinkByType("SoilWat");
            Soil.Get("sw_dep", out SWlayers);
            SWDeps[i] = (double)MathUtility.Sum(SWlayers);
        }

        TotalSW = (double)MathUtility.Sum(SWDeps) ;
        for (int i = 0; i < ZoneNames.Length; i++)
            SoilWaters.Add(ZoneNames[i], SWDeps[i] / TotalSW * RootData.Zone[i].ZoneArea);

        return SoilWaters;
    }

    /// <summary>
    /// Calculate how deep roots are in a given layer as a proportion of the layer depth.
    /// </summary>
    /// <param name="layer">Layer number</param>
    /// <param name="root_depth">Depth of the roots</param>
    /// <param name="dlayer">Array of layer depths</param>
    /// <returns>Depth of the roots in the layer as percentage of the layer depth</returns>
    private double RootProportion(int layer, double root_depth, double[] dlayer)
    {
        double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
        double depth_to_layer_top = 0;      // depth to top of layer (mm)
        double depth_to_root = 0;           // depth to root in layer (mm)
        double depth_of_root_in_layer = 0;  // depth of root within layer (mm)
        // Implementation Section ----------------------------------
        for (int i = 0; i <= layer; i++)
            depth_to_layer_bottom += dlayer[i];
        depth_to_layer_top = depth_to_layer_bottom - dlayer[layer];
        depth_to_root = Math.Min(depth_to_layer_bottom, root_depth);
        depth_of_root_in_layer = (double)Math.Max(0.0, depth_to_root - depth_to_layer_top);

        return depth_of_root_in_layer / dlayer[layer];
    }

    /// <summary>
    /// Calculate the deepest layer containing roots.
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="root_depth"></param>
    /// <param name="dlayer"></param>
    /// <returns>The index of the deepest layer containing roots</returns>
    private int CalcMaxRootLayer(double root_depth, double[] dlayer)
    {
        double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
        for (int i = 0; i < dlayer.Length; i++)
        {
            depth_to_layer_bottom += dlayer[i];
            if (root_depth < depth_to_layer_bottom)
                return i;
        }

        return dlayer.Length; //bottom layer
    }
}