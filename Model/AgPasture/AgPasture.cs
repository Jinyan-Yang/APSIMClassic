using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Schema;
using ModelFramework;
using CSGeneral;


/// <summary>
/// A multi-species pasture model
/// </summary>
public class AgPasture
{
    #region Parameters for initialisation  --------------------------------------------------------

    //// - general parameters (for sward or initialisation only)  ---------------------------------

    [Link]
    private Component My = null;

    [Description("Name of the sward mix")]
    private string thisCropName = "";

    [Param]
    [Description("Name of species to simulate")]
    [Units("")]
    private string[] speciesToSimulate = null;

    [Param]
    [Description("Whether water uptake is calculated by agpasture or apsim")]
    [Units("calc/apsim")]
    private string WaterUptakeSource = "calc";
    [Param]
    [Description("Whether N uptake is calculated by agpasture or apsim")]
    [Units("calc/apsim")]
    private string NUptakeSource = "calc";

    private bool usingSpeciesHeight = false;
    [Param]
    [Description("Whether plant height is determined for each species, instead of avg sward")]
    [Units("yes/no")]
    private string UseHeightBySpecies
    {
        get
        {
            if (usingSpeciesHeight)
                return "yes";
            else
                return "no";
        }
        set
        {
            usingSpeciesHeight = value.ToLower() == "yes";
        }
    }

    private bool usingSpeciesRoot = false;
    [Param]
    [Description("Whether root distribution is determined for each species, instead of avg sward")]
    [Units("yes/no")]
    private string UseRootBySpecies
    {
        get
        {
            if (usingSpeciesRoot)
                return "yes";
            else
                return "no";
        }
        set
        {
            usingSpeciesRoot = value.ToLower() == "yes";
        }
    }

    private bool usingSpeciesPhotosynthesis = false;
    [Param]
    [Description("Whether photosynthesis is computed by species or average sward")]
    [Units("yes/no")]
    private string UsePhotosynthesisBySpecies
    {
        get
        {
            if (usingSpeciesPhotosynthesis)
                return "yes";
            else
                return "no";
        }
        set
        {
            usingSpeciesPhotosynthesis = value.ToLower() == "yes";
        }
    }

    private bool usingWAvailableBySpecies = false;
    [Param]
    [Description("Whether the water availability is determined by species, instead of whole sward")]
    [Units("yes/no")]
    private string UseWaterAvailableBySpecies
    {
        get
        {
            if (usingWAvailableBySpecies)
                return "yes";
            else
                return "no";
        }
        set
        {
            usingWAvailableBySpecies = value.ToLower() == "yes";
        }
    }

    private bool usingAlternativeWAvailability = false;
    [Param]
    [Description("Whether alternative method for determining plant available water is to be used")]
    [Units("yes/no")]
    private string UseAlternativeWaterAvailable
    {
        get
        {
            if (usingAlternativeWAvailability)
                return "yes";
            else
                return "no";
        }
        set
        {
            usingAlternativeWAvailability = value.ToLower() == "yes";
        }
    }

    private bool usingAlternativeWUptake = false;
    [Param]
    [Description("Whether alternative method for determining plant water uptake is to be used")]
    [Units("yes/no")]
    private string UseAlternativeWaterUptake
    {
        get
        {
            if (usingAlternativeWUptake)
                return "yes";
            else
                return "no";
        }
        set
        {
            usingAlternativeWUptake = value.ToLower() == "yes";
        }
    }

    private bool usingNAvailableBySpecies = false;
    [Param]
    [Description("Whether the N availability is determined by species, instead of whole sward")]
    [Units("yes/no")]
    private string UseNAvailableBySpecies
    {
        get
        {
            if (usingNAvailableBySpecies)
                return "yes";
            else
                return "no";
        }
        set
        {
            usingNAvailableBySpecies = value.ToLower() == "yes";
        }
    }

    private int NExtractabilityMethod = 0;
    [Param]
    [Description("The index marking the method used for determining plant available N")]
    [Units("0-3")]
    private double NAvailabilityMethod
    {
        get
        {
            return NExtractabilityMethod;
        }
        set
        {
            NExtractabilityMethod = (int)value;
        }
    }

    private bool usingAlternativeNUptake = false;
    [Param]
    [Description("Whether alternative method for determining plant N uptake is to be used")]
    [Units("yes/no")]
    private string UseAlternativeNUptake
    {
        get
        {
            if (usingAlternativeNUptake)
                return "yes";
            else
                return "no";
        }
        set
        {
            usingAlternativeNUptake = value.ToLower() == "yes";
        }
    }

    [Param]
    [Description("Maximum NH4 uptake rate for each species")]
    [Units("ppm/day")]
    private double[] MaximumUptakeRateNH4;

    [Param]
    [Description("Maximum NO3 uptake rate for each species")]
    [Units("ppm/day")]
    private double[] MaximumUptakeRateNO3;

    [Param]
    [Description("Reference root length density for water and N uptake")]
    [Units("mm/mm3")]
    private double[] referenceRLD;

    [Param]
    [Description("Coefficient for NH4 availability")]
    [Units("0-1")]
    private double[] kNH4;

    [Param]
    [Description("Coefficient for NO3 availability")]
    [Units("0-1")]
    private double[] kNO3;

    [Param]
    [Description("Exponent factor of function determining soil extractable N (approach01)")]
    [Units("0-1")]
    private double[] NextraSWF;

    private bool usingPairWise;
    [Param]
    [Description("Whether old pair-wise function to compute plant height is to be used")]
    [Units("yes/no")]
    private string UsePairWise
    {
        get
        {
            if (usingPairWise)
                return "yes";
            else
                return "no";
        }
        set
        {
            usingPairWise = value.ToLower() == "yes";
        }
    }

    ////  >> Initial values >>>
    //  (to be input from interface, will overwrite the values of dmshoot, dmroot, rootdepth, and root dist. params)
    [Param(IsOptional = true)]
    private double[] iniShootDM = null;
    [Param(IsOptional = true)]
    private double[] iniRootDM = null;
    [Param(IsOptional = true)]
    private double[] iniRootDepth = null;

    [Param(IsOptional = true)]
    private double[] iniRootDepthParam = null;
    [Param(IsOptional = true)]
    private double[] iniRootCurveParam = null;

    //// - Parameter for pasture species (all arrays)  --------------------------------------------

    ////  >> General parameters >>>
    [Param]
    [Description("Actual name of each species")]
    [Units("")]
    private string[] speciesName = null;
    [Param]
    [Description("Plant type for micromet")]
    [Units("")]
    private string[] micrometType = null;
    [Param]
    [Description("Photosynthesis pathway (C3 or C4)")]
    [Units("3/4")]
    private double[] photoPath = null;
    [Param]
    [Description("Whether the species is legume (1=yes, 0=no)")]
    [Units("0/1")]
    private double[] isLegume = null;

    ////  >> Potential growth parameters >>>
    [Param]
    [Description("Reference leaf C assimilation during photosynthesis")]
    [Units("mgCO2/m^2/s")]
    private double[] Pm;
    [Param]
    [Description("Pgrowth/Pgross")]
    [Units("")]
    private double[] growthEfficiency;
    [Param]
    [Description("Maintenance respiration")]
    [Units("%")]
    private double[] maintRespiration;
    [Param]
    [Description("Leaf gross photosynthesis rate")]
    [Units("mg CO2/J")]
    private double[] alphaPhoto;
    [Param]
    [Description("photosynthesis curvature parameter")]
    [Units("J/kg/s")]
    private double[] thetaPhoto;

    [Param]
    [Description("Light extinction coefficient")]
    [Units("")]
    private double[] lightExtCoeff;

    [Param]
    [Description("Minimum temperature for growth")]
    [Units("")]
    private double[] growthTmin;
    [Param]
    [Description("Optimum temperature for growth")]
    [Units("")]
    private double[] growthTopt;
    [Param]
    [Description("reference temperature for growth")]
    [Units("")]
    private double[] growthTref;
    [Param]
    [Description("Coefficient q on temperature function for plant growth")]
    [Units("")]
    private double[] growthTq;

    [Param]
    [Description("whether heat stress will be use")]
    [Units("")]
    private string[] useHeatStress;
    [Param]
    [Description("onset temperature for heat effects")]
    [Units("")]
    private double[] heatOnsetT;
    [Param]
    [Description("full temperature for heat effects")]
    [Units("")]
    private double[] heatFullT;
    [Param]
    [Description("temperature sum for recovery - sum of (25-mean)")]
    [Units("")]
    private double[] heatSumT;
    [Param]
    [Description("")]
    [Units("oC")]
    private double[] heatRecoverT;
    [Param]
    [Description("")]
    [Units("")]
    private double[] heatTq;

    [Param]
    [Description("whether heat stress will be use")]
    [Units("")]
    private string[] useColdStress;
    [Param]
    [Description("onset temperature for cold effects")]
    [Units("")]
    private double[] coldOnsetT;
    [Param]
    [Description("full temperature for cold effects")]
    [Units("")]
    private double[] coldFullT;
    [Param]
    [Description("temperature sum for recovery - sum of means")]
    [Units("")]
    private double[] coldSumT;
    [Param]
    [Description("")]
    [Units("oC")]
    private double[] coldRecoverT;
    [Param]
    [Description("")]
    [Units("")]
    private double[] coldTq;

    [Param]
    [Description("Base CO2 content in atmosphere")]
    [Units("")]
    private double[] referenceCO2;
    [Param]
    [Description("")]
    [Units("")]
    private double[] CO2PmaxScale;
    [Param]
    [Description("")]
    [Units("")]
    private double[] CO2NScale;
    [Param]
    [Description("")]
    [Units("")]
    private double[] CO2NMin;
    [Param]
    [Description("")]
    [Units("")]
    private double[] CO2NCurvature;

    ////  >> Partition of new growth  >>>
    // - Shoot:root partition
    [Param]
    [Description("Target or ideal plant's shoot:root ratio")]
    [Units("-")]
    private double[] targetSRratio;
    [Param]
    [Description("Maximum fraction of biomass allocated to roots")]
    [Units("0-1")]
    private double[] maxRootFraction;
    [Param]
    [Description("Factor for increasing DM allocation to shoot during reproductive growth")]
    [Units("0-1")]
    private double[] allocationSeasonF;
    [Param]
    [Description("")]
    [Units("0-1")]
    private double[] StartHighAllocation;
    [Param]
    [Description("")]
    [Units("0-1")]
    private double[] DurationHighAllocation;
    [Param]
    [Description("")]
    [Units("0-1")]
    private double[] ShoulderHighAllocation;

    // - Alternative account for seasonal S:R partition
    [Param]
    [Description("Whether DM allocation (shoot/root) will be adjusted using latitude")]
    [Units("yes/no")]
    private string[] useLatitudeFunction;
    [Param]
    [Description("Reference latitude, beyond which DM allocation is hardly affected")]
    [Units("degrees")]
    private double[] ReferenceLatitude;
    [Param]
    [Description("Exponent of function for defining the start of period with high allocation to shoot")]
    [Units("-")]
    private double[] paramALatFunction;
    [Param]
    [Description("Define the duration of onset phase with high allocation, fraction of main phase")]
    [Units("0-1")]
    private double[] onsetFacLatFunction;
    [Param]
    [Description("Define the duration of outset phase with high allocation, fraction of main phase")]
    [Units("0-1")]
    private double[] outsetFacLatFunction;
    [Param]
    [Description("Maximum duration of the shoulder phases with high DM allocation")]
    [Units("days")]
    private double[] maxShoulderLatFunction;
    [Param]
    [Description("Minimum duration of main phase (plateau) with high DM allocation")]
    [Units("days")]
    private double[] minPlateauLatFunction;
    [Param]
    [Description("Exponent of function defining the duration of main phase with high allocation")]
    [Units("days")]
    private double[] paramBLatFunction;
    [Param]
    [Description("Maximum increase in DM allocation to shoot during reproductive growth")]
    [Units("0-1")]
    private double[] allocationMax;
    [Param]
    [Description("Exponent of function defining the duration of main phase with high allocation")]
    [Units("days")]
    private double[] paramCLatFunction;

    // - Partition of shoot DM into leaves
    [Param]
    [Description("Fraction of new growth allocated to leaf (0-1)")]
    [Units("")]
    private double[] maxFLeaf;
    [Param]
    [Description("")]
    [Units("")]
    private double[] minFLeaf;
    [Param]
    [Description("")]
    [Units("")]
    private double[] dmMaxFLeaf;
    [Param]
    [Description("")]
    [Units("")]
    private double[] dmReferenceFLeaf;
    [Param]
    [Description("")]
    [Units("")]
    private double[] exponentFLeaf;

    [Param]
    [Description("Fraction of new growth allocated to stolon (0-1)")]
    [Units("")]
    private double[] fStolon;

    private double[] specificLeafArea;
    [Param]
    [Description("Specific leaf area, per dry matter weight")]
    [Units("m^2/kgDM")]
    private double[] SpecificLeafArea
    {
        get
        {
            return specificLeafArea;
        }
        set
        {
            specificLeafArea = new double[value.Length];
            for (int s = 0; s < value.Length; s++)
            { specificLeafArea[s] = value[s]; }
        }
    }

    private double[] specificRootLength;
    [Param]
    [Description("Specific root length, per dry matter weight")]
    [Units("m/gDM")]
    private double[] SpecificRootLength
    {
        get
        {
            return specificRootLength;
        }
        set
        {
            specificRootLength = new double[value.Length];
            for (int s = 0; s < value.Length; s++)
            { specificRootLength[s] = value[s]; }
        }
    }

    ////  >> Tissue turnover and senescence >>>
    [Param]
    [Description("Number of live leaves per tiller")]
    [Units("")]
    private double[] liveLeavesPerTiller;

    [Param]
    [Description("Factor for adjusting DM turnover of growing tissue")]
    [Units("<0.0")]
    private double[] facGrowingTissue;
    [Param]
    [Description("Senescence rate for shoot (live to dead material)")]
    [Units("")]
    private double[] rateLive2Dead;
    [Param]
    [Description("Senescence rate for stolon (live to dead material)")]
    [Units("")]
    private double[] refTurnoverRateStolon;
    [Param]
    [Description("Littering rate (dead to litter)")]
    [Units("")]
    private double[] rateDead2Litter;
    [Param]
    [Description("Senescence rate for roots (live to soil FOM)")]
    [Units("")]
    private double[] rateRootSen;

    [Param]
    [Description("Mass flux minimum temperature")]
    [Units("")]
    private double[] massFluxTmin;
    [Param]
    [Description("Mass flux optimum temperature")]
    [Units("")]
    private double[] massFluxTopt;
    [Param]
    [Description("")]
    [Units("")]
    private double[] massFluxTq;
    [Param(MinVal = 1.0)]
    [Description("Mass flux scale factor at GLFwater=0.0")]
    [Units("")]
    private double[] massFluxW0;
    [Param]
    [Description("Mass flux optimum GLFwater=0.5")]
    [Units("")]
    private double[] massFluxWopt;
    [Param]
    [Description("Mass flux exponent")]
    [Units("")]
    private double[] massFluxDeadWq;

    [Param]
    [Description("Parameter for stock influence on senescence")]
    [Units("")]
    private double[] stockParameter;
    [Param]
    [Description("Coefficient for using luxury N from tissue 2 to Nremob")]
    [Units("0-1")]
    private double[] Kappa2_Remob;
    [Param]
    [Description("Coefficient for using luxury N from tissue 3 to Nremob")]
    [Units("0-1")]
    private double[] Kappa3_Remob;
    [Param]
    [Description("Coefficient for partitioning non-used Nremob into tissue 4")]
    [Units("0-1")]
    private double[] Kappa4_Remob;

    ////  >> Digestibility and feed quality
    [Param]
    [Description("Digestibility of live plant material (0-1)")]
    [Units("")]
    private double[] digestLive;
    [Param]
    [Description("Digestibility of dead plant material (0-1)")]
    [Units("")]
    private double[] digestDead;

    ////  >> Default values for DM
    [Param]
    [Description("Shoot dry weight")]
    [Units("kgDM/ha")]
    private double[] dmshoot;  //default initial shoot mass (RCichota May 2014, change from dmtotal to dmshoot)

    [Param]
    [Description("Root dry weight")]
    [Units("kgDM/ha")]
    private double[] dmroot;

    [Param]
    [Description("Minimum green DM")]
    [Units("kgDM/ha")]
    private double[] dmgreenmin;
    [Param]
    [Description("Minimum dead DM")]
    [Units("kgDM/ha")]
    private double[] dmdeadmin;

    [Param]
    [Output]
    [Description("Fractions of initial dmshoot for each biomass pool, for non-legumes")]
    [Units("0-1")]
    private double[] initialDMFractions_grass;
    [Param]
    [Output]
    [Description("Fractions of initial dmshoot for each biomass pool, for legume species")]
    [Units("0-1")]
    private double[] initialDMFractions_legume;

    ////  >> N concentration in plant tissues >>>
    [Param]
    [Description("Optimum N concentration of leaves (no stress)")]
    [Units("%")]
    private double[] NconcOptimum_leaves;
    [Param]
    [Description("Maximum N concentration of leaves (luxury uptake)")]
    [Units("%")]
    private double[] NconcMaximum_leaves;
    [Param]
    [Description("Minimum N concentration of leaves (dead leaves)")]
    [Units("%")]
    private double[] NconcMinimum_leaves;
    [Param]
    [Description("N concentration for stems, relative to leaves")]
    [Units("0-1")]
    private double[] RelativeNconc_Stems;
    [Param]
    [Description("N concentration for stolons, relative to leaves")]
    [Units("0-1")]
    private double[] RelativeNconc_Stolons;
    [Param]
    [Description("N concentration for roots, relative to leaves")]
    [Units("0-1")]
    private double[] RelativeNconc_Roots;
    [Param]
    [Description("N concentration for plants at stage 2 (developing), relative to optimum")]
    [Units("0-1")]
    private double[] RelativeNconc_stage2;
    [Param]
    [Description("N concentration for plants at stage 3 (mature), relative to optimum")]
    [Units("0-1")]
    private double[] RelativeNconc_stage3;

    ////  >> N fixation (for legumes) >>>
    [Param]
    [Description("Minimum fraction of N demand fixed by legumes")]
    [Units("")]
    private double[] NMinFix;
    [Param]
    [Description("Maximum fraction of N demand fixed by legumes")]
    [Units("")]
    private double[] NMaxFix;

    ////  >> modifiers for growth limiting factors >>>
    [Param]
    [Description("Coefficient for modifying the effect of N stress on plant growth")]
    [Units("")]
    private double[] NdilutCoeff;

    [Param]
    [Description("Effects of water uptake/demand on plant growth")]
    [Units("0-1")]
    private double[] waterStressFactor;
    [Param]
    [Description("Soil moisture saturation effects on growth")]
    [Units("0-1")]
    private double[] soilSatFactor;

    [Param]
    [Description("Generic growth limiting factor")]
    [Units("0-1")]
    private double[] GenericGLF;

    [Param]
    [Description("Generic, soil related, growth limiting factor")]
    [Units("0-1")]
    private double[] SFertilityGLF;

    ////  >> Preferences for species and parts (on removal)
    [Param]
    [Description("Weight factor defining the preference level for green DM")]
    private double[] PreferenceForGreenDM;
    [Param]
    [Description("Weight factor defining the preference level for dead DM")]
    private double[] PreferenceForDeadDM;
    [Param]
    [Description("Weight factor defining the preference level for dead DM")]
    private double[] PreferenceForLeaves;

    ////  >> Root depth and distribution >>>
    /// <summary>
    /// Current root depth (mm)
    /// </summary>
    private double[] myRootDepth;
    [Param]
    [Description("Initial root depth")]
    [Units("mm")]
    private double[] rootDepth
    {
        get { return myRootDepth; }
        set { myRootDepth = value; }
    }

    private int p_RootDistributionMethod = 0;
    [Param]
    [Output]
    [Description("Root distribution method")]
    [Units("")]
    private string RootDistributionMethod
    {
        get
        {
            switch (p_RootDistributionMethod)
            {
                case 1:
                    return "UserDefined";
                case 2:
                    return "ExpoLinear";
                default:
                    // case = 0
                    return "Homogeneous";
            }
        }
        set
        {
            if (value.ToLower() == "userdefined")
                p_RootDistributionMethod = 1;
            else if (value.ToLower() == "expolinear")
                p_RootDistributionMethod = 2;
            else      // default = homogeneous
                p_RootDistributionMethod = 0;
        }
    }

    private double[] p_ExpoLinearDepthParam;
    [Param]
    [Output]
    [Description("Fraction of root depth where its proportion starts to decrease")]
    [Units("0-1")]
    private double[] ExpoLinearDepthParam
    {
        get { return p_ExpoLinearDepthParam; }
        set
        {
            p_ExpoLinearDepthParam = new double[value.Length];
            for (int i = 0; i < value.Length; i++)
                p_ExpoLinearDepthParam[i] = value[i];
            if (p_ExpoLinearDepthParam[0] == 1.0)
                p_RootDistributionMethod = 0;    // effectively it defines a homogeneous distribution
        }
    }

    private double[] p_ExpoLinearCurveParam;
    [Param]
    [Output]
    [Description("Exponent to determine mass distribution in the soil profile")]
    [Units("")]
    private double[] ExpoLinearCurveParam
    {
        get { return p_ExpoLinearCurveParam; }
        set
        {
            p_ExpoLinearCurveParam = new double[value.Length];
            for (int i = 0; i < value.Length; i++)
                p_ExpoLinearCurveParam[i] = value[i];
            if (p_ExpoLinearCurveParam[0] == 0.0)
                p_RootDistributionMethod = 0;   // It is impossible to solve, but its limit is a homogeneous distribution
        }
    }

    //// >> Plant height >>>
    [Param]
    [Description("Maximum average height for each species in the sward")]
    [Units("mm")]
    private double[] MaxPlantHeight;
    [Param]
    [Description("Mass above ground when maximum height is reached")]
    [Units("kgDM/ha")]
    private double[] MassForMaxHeight;
    [Param]
    [Description("Exponent of function describing plant height as function of DM weight")]
    [Units(">1.0")]
    private double[] ExponentHeightFromMass;
    [Param]
    [Description("Minimum plant height, for all species")]
    [Units("mm")]
    private double MinimumHeight;

    [Link]
    private LinearInterpolation HeightMassFN = null;

    ////  >> Soil related (water uptake) >>>
    [Param]
    [Output]
    [Description("Relative root length density")]
    [Units("0-1")]
    private double[] rlvp = null;
    [Param]
    private double[] kl = null;      //SW uptake parameter (/day)
    [Param]
    private double[] ll = null;      //Crop Lower Limit (mm/mm)

    [Param]
    [Description("Exploration factor, for each soil layer - affects root growth")]
    [Units("0-1")]
    private double[] xf = null;

    ////  >> Additional functions >>>
    [Link]
    private LinearInterpolation FVPDFunction = null;    //Senescence rate is affected by min(gf-N, gf_water)

    //// --  Parameters for annual species  -------------------------------------------------------
    //  these were de-activated (hidden) as they are not really used and some procedure were
    //   never really implemented  (RCichota, Oct/2014)
    //[Param]
    [Description("Species type (1=annual,0=perennial)")]
    [Units("0/1")]
    private double[] isAnnual = null;
    //[Param]
    [Description("Earliest day of emergence (for annuals only)")]
    [Units("")]
    private double[] dayEmerg;
    //[Param]
    [Description("Earliest month of emergence (for annuals only)")]
    [Units("")]
    private double[] monEmerg;
    //[Param]
    [Description("Earliest day of anthesis (for annuals only)")]
    [Units("")]
    private double[] dayAnth;
    //[Param]
    [Description("Earliest month of anthesis (for annuals only)")]
    [Units("")]
    private double[] monAnth;
    //[Param]
    [Description("Days from anthesis to maturity (for annuals only)")]
    [Units("")]
    private double[] daysToMature;

    //[Param]
    [Description("Daily root growth")]
    [Units("(mm)")]
    private double[] dRootDepth;
    //[Param]
    [Description("Maximum root depth")]
    [Units("(mm)")]
    private double[] maxRootDepth;
    //// ------------------------------------------------------------------------------------------

    #endregion  -----------------------------------------------------------------------------------

    #region Inputs from othe modules  -------------------------------------------------------------

    [Link]
    private Clock myClock;
    [Link]
    private MetFile MetData;

    [Input]
    private double[] dlayer;   //Soil Layer Thickness (mm)
    [Input]
    private double[] sw_dep;  //soil water by layer
    [Input]
    private double[] sw;      //soil water content by layer
    [Input]
    private double[] SAT_dep;     //saturation point
    [Input]
    private double[] DUL;     //drainage upper limit (field capacity);
    [Input]
    private double[] nh4;     //SNH4dep = new float[dlayer.Length];
    [Input]
    private double[] no3;     //SNO3dep = new float[dlayer.Length];
    [Input]
    private double[] bd;    // soil bulk density

    // - Making AgPasture patch-aware  ---------------------------------------------
    [Input(IsOptional = true)]
    private double[] nh4_PlantAvailable;
    [Input(IsOptional = true)]
    private double[] no3_PlantAvailable;

    [Input(IsOptional = true)]
    private double[] PatchArea;

    [Input(IsOptional = true)]
    private CNPatchVariableType PatchNH4;
    [Input(IsOptional = true)]
    private CNPatchVariableType PatchNO3;

    // -----------------------------------------------------------------------------

    /// <summary>CO2 in atmosphere</summary>
    [Input(IsOptional = true)]
    [Description("Actual CO2, updated from met and ClimateControl")]
    [Units("")]
    public double co2 = 380;

    /// <summary>
    /// Gets or sets the effective stocking rate, to calculate trampling on pasture (increase senescence)
    /// </summary>
    public double StockRate
    {
        get { return Species.stockingRate; }
        set { Species.stockingRate = value; }
    }

    #endregion  -----------------------------------------------------------------------------------

    #region General internal variables  ------------------------------------------------------------

    /// <summary>
    /// The collection of pasture species in the sward
    /// </summary>
    private Species[] mySpecies;

    /// <summary>
    /// Number of species in the sward
    /// </summary>
    private int NumSpecies = 0;

    /// <summary>
    /// Basic state parameters for each species
    /// </summary>
    private SpeciesStateSettings[] InitialState;

    //// Pasture sward parameters, aggregated over all species

    /// <summary>Daily potential growth (kg/ha)</summary>
    private double swardPotentialGrowth;
    /// <summary>Daily potential growth, after water stress (kg/ha)</summary>
    private double swardPotGrowthWater;
    /// <summary>Daily actual growth (kg/ha)</summary>
    private double swardActualGrowth;
    /// <summary>Daily growth above ground (kg/ha)</summary>
    private double swardHerbageGrowth;
    /// <summary>Daily litter formation (kg/ha)</summary>
    private double swardLitterDM;
    /// <summary>Daily root senescence (kg/ha)</summary>
    private double swardSenescedRootDM;
    /// <summary>Amount of N in litter (kgN/ha)</summary>
    private double swardLitterN;
    /// <summary>Amount of N in root senesced (kgN/ha)</summary>
    private double swardSenescedRootN;

    /// <summary>Sward average canopy height (mm)</summary>
    private double swardHeight;
    /// <summary>Sward average green LAI</summary>
    private double swardGreenLAI;
    /// <summary>Sward average dead LAI</summary>
    private double swardDeadLAI;
    /// <summary>Sward average total LAI</summary>
    private double swardTotalLAI;
    /// <summary>Sward average light extinction coefficient</summary>
    private double swardLightExtCoeff;
    /// <summary>Sward total green shoot DM (kg/ha)</summary>
    private double swardGreenDM;
    /// <summary>Sward total dead shoot DM (kg/ha)</summary>
    private double swardDeadDM;
    /// <summary>Sward total shoot DM (kg/ha)</summary>
    private double swardShootDM;
    /// <summary>Sward total root DM (kg/ha)</summary>
    private double swardRootDM;

    /// <summary>sward average root zone depth (mm)</summary>
    private double swardRootDepth = 0.0;
    /// <summary>Soil layer at bottom of root zone</summary>
    private int swardRootZoneBottomLayer;

    /// <summary>Plant available soil water for each layer (mm)</summary>
    private double[] soilAvailableWater;
    /// <summary>Daily total soil water demand (mm)</summary>
    private double swardWaterDemand;
    /// <summary>Soil water taken up from each layer (mm)</summary>
    private double[] soilWaterUptake;

    /// <summary>Soil water uptake as given by an external module (mm)</summary>
    private double[] swardWaterUptakeByAPSIM;


    /// <summary>Soil NH4_N uptake as given by an external module (kgN/ha)</summary>
    private double[] swardNH4UptakeByAPSIM;
    /// <summary>Soil NO3_N uptake as given by an external module (kgN/ha)</summary>
    private double[] swardNO3UptakeByAPSIM;

    /// <summary>Amount of N taken up from each soil layer (kgN/ha)</summary>
    private double[] soilNH4Available;
    /// <summary>Amount of N taken up from each soil layer (kgN/ha)</summary>
    private double[] soilNO3Available;
    /// <summary>Total plant available N in soil (kgN/ha)</summary>
    private double swardSoilNavailable;
    /// <summary>Plant N demand from soil (kgN/ha)</summary>
    private double swardSoilNDemand;

    /// <summary>Amount of N taken up from each soil layer (kgN/ha)</summary>
    private double[] soilNH4Uptake;
    /// <summary>Amount of N taken up from each soil layer (kgN/ha)</summary>
    private double[] soilNO3Uptake;

    /// <summary>Amount of N fixed by legumes</summary>
    private double swardNFixed = 0.0;
    /// <summary>Growth limiting factor due to soil nitrogen</summary>
    private double swardGLFN;
    /// <summary>Growth limiting factor due to soil water</summary>
    private double swardGLFWater;
    /// <summary>Growth limiting factor due to ambient temperature</summary>
    private double swardGLFTemp;

    /// <summary>Amount of DM harvested</summary>
    private double swardHarvestedDM;
    /// <summary>Amount of N harvested</summary>
    private double swardHarvestedN;
    /// <summary>Digestibility of harvested material</summary>
    private double swardHarvestDigestibility;

    // Constants ..................................................................................

    /// <summary>C fraction on DM, for conversion</summary>
    const double CarbonFractionDM = 0.4;

    #endregion  -----------------------------------------------------------------------------------

    #region Events to be invoked  -----------------------------------------------------------------

    /// <summary>NewCrop event</summary>
    [Event]
    public event NewCropDelegate NewCrop;

    /// <summary>New_Canopy event</summary>
    [Event]
    public event NewCanopyDelegate New_Canopy;

    /// <summary>NewPotentialGrowth event</summary>
    [Event]
    public event NewPotentialGrowthDelegate NewPotentialGrowth;

    /// <summary>IncorpFOM event</summary>
    [Event]
    public event FOMLayerDelegate IncorpFOM;

    /// <summary>BiomassRemoved event</summary>
    [Event]
    public event BiomassRemovedDelegate BiomassRemoved;

    /// <summary>WaterChanged event</summary>
    [Event]
    public event WaterChangedDelegate WaterChanged;

    /// <summary>NitrogenChanged event</summary>
    [Event]
    public event NitrogenChangedDelegate NitrogenChanged;

    /// <summary>NitrogenChanged event</summary>
    [Event]
    public event AddSoilCNPatchDelegate AddSoilCNPatch;

    #endregion  -----------------------------------------------------------------------------------

    #region Initialisation methods  ---------------------------------------------------------------

    /// <summary>
    /// Eventhandler - initialisation
    /// </summary>
    [EventHandler]
    public void OnInit2()
    {
        // Init parameters after reading the data
        thisCropName = My.Name;
        InitParameters();

        // Pass on some sward variable to each species
        SetSpeciesWithSwardData();

        // Tell other modules that I exist
        AdvertiseThisCrop();

        // Send info about canopy - needed here to proper initialise micromet
        DoNewCanopyEvent();

        // write some basic initialisation info
        writeSummary();
    }

    /// <summary>
    /// Check and initialise sward and species parameters
    /// </summary>
    private void InitParameters()
    {
        //// Checks which species will be simulated  --------------------------------------------------
        //        . added by RCichota, Oct/2014

        // get the number of species to be simulated
        NumSpecies = speciesToSimulate.Length;

        // check number of species - no more than those we have parameters for (given here by speciesName)
        if (NumSpecies > speciesName.Length)
            throw new Exception("Number of species to simulate is greater than the number of species for which parameters were given");
        if (NumSpecies < 1)
            throw new Exception("Number of species to simulate is zero");

        // check for duplicates and whether species have been parameterised
        for (int s1 = 0; s1 < NumSpecies; s1++)
        {
            for (int s2 = s1 + 1; s2 < NumSpecies; s2++)
            {
                if (speciesToSimulate[s2].ToLower() == speciesToSimulate[s1].ToLower())
                    throw new Exception("The name \"" + speciesToSimulate[s1] + "\" was given more than once. Only one is allowed");
            }

            int myCount = 0;
            for (int s2 = 0; s2 < speciesName.Length; s2++)
            {
                if (speciesToSimulate[s1].ToLower() == speciesName[s2].ToLower())
                    myCount += 1;
            }

            if (myCount < 1)
                throw new Exception("The name \"" + speciesToSimulate[s1] + "\" does not correspond to any parameterised species, check spelling");
        }
        //// --------------------------------------------------------------------------------------

        // make sure that DM fractions for initialisation have the right number of values (cut excess or add zeroes)
        //   there are 12 pools 4 for leaves, 4 for stems, and 3 for stolons
        Array.Resize(ref initialDMFractions_grass, 11);
        Array.Resize(ref initialDMFractions_legume, 11);

        // check whether values for root distribution parameters were given for each species (over-write the default ones)
        if (iniRootDepthParam != null)
        {
            if (iniRootDepthParam.Length < NumSpecies)
                throw new Exception("Number of values for paramater \"iniRootDepthParam\" was smaller than number of species");
            else
                ExpoLinearDepthParam = iniRootDepthParam;
        }

        if (iniRootCurveParam != null)
        {
            if (iniRootCurveParam.Length < NumSpecies)
                throw new Exception("Number of values for paramater \"iniRootCurveParam\" was smaller than number of species");
            else
                ExpoLinearCurveParam = iniRootCurveParam;
        }

        // Number of layers
        int nLayers = dlayer.Length;

        // check whether valkues for kNO3 and kNH4 were given for all layers
        if (kNH4.Length == 1)
        { // if only one value was given, assume homogeneous over the profile
            Array.Resize(ref kNH4, nLayers);
            for (int layer = 1; layer < nLayers; layer++)
                kNH4[layer] = kNH4[0];
        }
        if (kNO3.Length == 1)
        {// if only one value was given, assume homogeneous over the profile
            Array.Resize(ref kNO3, nLayers);
            for (int layer = 1; layer < nLayers; layer++)
                kNO3[layer] = kNO3[0];
        }
        Array.Resize(ref kNH4, nLayers);
        Array.Resize(ref kNO3, nLayers);

        //// Create and initialise each species

        mySpecies = new Species[NumSpecies];
        InitialState = new SpeciesStateSettings[NumSpecies];

        // set links to static members (clock, MetData, dlayer, CO2, etc)
        Species.Clock = myClock;
        Species.MetFile = MetData;
        Species.CO2 = co2;

        for (int s1 = 0; s1 < NumSpecies; s1++)
        {
            for (int s2 = 0; s2 < speciesName.Length; s2++)
            {
                if (speciesName[s2].ToLower() == speciesToSimulate[s1].ToLower())
                {
                    // create species and add to array
                    mySpecies[s1] = new Species();

                    // pass on values for dlayer
                    mySpecies[s1].dlayer = dlayer;

                    // set the parameters and initialise the species
                    SetSpeciesParameters(s1, s2);

                    // Set the initial values for the state parameters, will needed this in case of reset
                    InitialState[s1] = new SpeciesStateSettings();
                    if (iniShootDM[s1] > 0.0)
                        InitialState[s1].ShootDM = iniShootDM[s1];
                    else
                        InitialState[s1].ShootDM = dmshoot[s2];

                    if (iniRootDM[s1] > 0.0)
                    {
                        InitialState[s1].RootDM = iniRootDM[s1];
                    }
                    else
                    {
                        if (dmroot[s2] > 0.0)
                            InitialState[s1].RootDM = dmroot[s2];
                        else
                            InitialState[s1].RootDM = iniShootDM[s1] / mySpecies[s2].targetSRratio;
                    }

                    if (iniRootDepth[s1] > 0.0)     // also needed to determine the max root deth of species being simulated
                        InitialState[s1].RootDepth = iniRootDepth[s1];
                    else
                    {
                        InitialState[s1].RootDepth = myRootDepth[s2];
                        iniRootDepth[s1] = myRootDepth[s2];
                    }

                    if (mySpecies[s1].isLegume)
                        InitialState[s1].DMFraction = initialDMFractions_legume;
                    else
                        InitialState[s1].DMFraction = initialDMFractions_grass;

                    InitialState[s1].NConcentration[0] = mySpecies[s1].NcleafOpt;
                    InitialState[s1].NConcentration[1] = mySpecies[s1].NcleafOpt * mySpecies[s1].NcRel2;
                    InitialState[s1].NConcentration[2] = mySpecies[s1].NcleafOpt * mySpecies[s1].NcRel3;
                    InitialState[s1].NConcentration[3] = mySpecies[s1].NcleafMin;
                    InitialState[s1].NConcentration[4] = mySpecies[s1].NcstemOpt;
                    InitialState[s1].NConcentration[5] = mySpecies[s1].NcstemOpt * mySpecies[s1].NcRel2;
                    InitialState[s1].NConcentration[6] = mySpecies[s1].NcstemOpt * mySpecies[s1].NcRel3;
                    InitialState[s1].NConcentration[7] = mySpecies[s1].NcstemMin;
                    InitialState[s1].NConcentration[8] = mySpecies[s1].NcstolOpt;
                    InitialState[s1].NConcentration[9] = mySpecies[s1].NcstolOpt * mySpecies[s1].NcRel2;
                    InitialState[s1].NConcentration[10] = mySpecies[s1].NcstolOpt * mySpecies[s1].NcRel3;
                    InitialState[s1].NConcentration[11] = mySpecies[s1].NcrootOpt;

                    break;
                }
            }
        }

        // set the initial state (DM, N, LAI, etc.) for the species
        for (int s = 0; s < NumSpecies; s++)
        {
            if (!usingSpeciesRoot)
            {  // Get the max root depth of all species
                InitialState[s].RootDepth = iniRootDepth.Max();
            }

            SetSpeciesState(s, InitialState[s]);
        }

        //// Initialising the aggregated parameters (whole sward)
        swardLightExtCoeff = 0.0;
        for (int s = 0; s < NumSpecies; s++)
        {
            //accumulate LAI of all species
            swardGreenLAI += mySpecies[s].greenLAI;
            swardDeadLAI += mySpecies[s].deadLAI;

            swardGreenDM += mySpecies[s].dmgreen;
            swardDeadDM += mySpecies[s].dmdead;

            //accumulate the sum for weighted average
            swardLightExtCoeff += mySpecies[s].lightExtCoeff * mySpecies[s].totalLAI;

            //Set the deepest root as sward depth
            if (mySpecies[s].rootDepth > swardRootDepth)
            {
                swardRootDepth = mySpecies[s].rootDepth;
                swardRootZoneBottomLayer = mySpecies[s].RootZoneBottomLayer();
            }

            swardRootDM += mySpecies[s].dmroot;
        }

        swardTotalLAI = swardGreenLAI + swardDeadLAI;
        swardShootDM = swardGreenDM + swardDeadDM;

        if (swardTotalLAI == 0.0)
        {
            swardLightExtCoeff = 1.0;
        }
        else
        {
            swardLightExtCoeff = swardLightExtCoeff / swardTotalLAI;
        }

        // Check plant height
        if (usingSpeciesHeight)
        { // each species has its own height
            swardHeight = mySpecies[0].height * mySpecies[0].dmshoot;
            for (int s = 1; s < NumSpecies; s++)
            {
                swardHeight += mySpecies[s].height * mySpecies[s].dmshoot;
            }

            swardHeight /= AboveGroundWt;
        }
        else
        { // only sward height is considered
            swardHeight = HeightfromDM();
            for (int s = 0; s < NumSpecies; s++)
            {
                mySpecies[s].height = swardHeight;
            }
        }

        // Check root distribution
        if (usingSpeciesRoot)
        { // each species has its own distribution
            RootFraction = new double[nLayers];
            for (int layer = 0; layer < nLayers; layer++)
            {
                for (int s = 0; s < NumSpecies; s++)
                {
                    RootFraction[layer] += mySpecies[s].dmroot * mySpecies[s].rootFraction[layer];
                }
                RootFraction[layer] /= RootWt;
            }
        }
        else
        {
            RootFraction = RootProfileDistribution(-1);
            for (int s = 0; s < NumSpecies; s++)
            {
                mySpecies[s].rootFraction = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; layer++)
                {
                    mySpecies[s].rootFraction[layer] = RootFraction[layer];
                }
            }
        }

        FractionToHarvest = new double[NumSpecies];
    }

    /// <summary>
    /// Set parameter values for each species in the sward
    /// </summary>
    /// <param name="s1">The index for the species being setup</param>
    /// <param name="s2">The index for the species in the parameter set</param>
    private void SetSpeciesParameters(int s1, int s2)
    {
        mySpecies[s1].speciesName = speciesName[s2];
        mySpecies[s1].micrometType = micrometType[s2];

        mySpecies[s1].isLegume = (int)isLegume[s2] == 1;
        mySpecies[s1].photoPath = "C" + (int)photoPath[s2];

        //// -- deactivating all the annual stuff (never really fully implemented) --------------------------
        // if (isAnnual[s] == 1) SP[s].isAnnual = true;
        // else SP[s].isAnnual = false;
        mySpecies[s1].isAnnual = false;

        mySpecies[s1].dayEmerg = 1; // (int)dayEmerg[s];
        mySpecies[s1].monEmerg = 1; //(int)monEmerg[s];
        mySpecies[s1].dayAnth = 1; //(int)dayAnth[s];
        mySpecies[s1].monAnth = 1; //(int)monAnth[s];
        mySpecies[s1].daysToMature = 1; //(int)daysToMature[s];
        if (mySpecies[s1].isAnnual) //calulate days from Emg to Antheis
            mySpecies[s1].CalcDaysEmgToAnth();
        mySpecies[s1].dRootDepth = 1; //(int)dRootDepth[s];
        mySpecies[s1].maxRootDepth = 10; //(int)maxRootDepth[s];
        //// ------------------------------------------------------------------------------------------------

        //// >> Potential growth (photosynthesis)  >>>
        mySpecies[s1].Pm = Pm[s2];                            //reference leaf co2 mg/m^2/s maximum
        mySpecies[s1].maintRespiration = maintRespiration[s2] * 0.01; // from % to fraction
        mySpecies[s1].growthEfficiency = growthEfficiency[s2];
        mySpecies[s1].alphaPhoto = alphaPhoto[s2];
        mySpecies[s1].thetaPhoto = thetaPhoto[s2];
        mySpecies[s1].lightExtCoeff = lightExtCoeff[s2];

        // Temperature, general effect and extreme, heat and cold effects
        mySpecies[s1].growthTmin = growthTmin[s2];
        mySpecies[s1].growthTopt = growthTopt[s2];
        mySpecies[s1].growthTref = growthTref[s2];
        mySpecies[s1].growthTq = growthTq[s2];
        mySpecies[s1].usingHeatStress = useHeatStress[s2].ToLower() == "yes";
        mySpecies[s1].heatOnsetT = heatOnsetT[s2];            //onset tempeature for heat effects
        mySpecies[s1].heatFullT = heatFullT[s2];            //full temperature for heat effects
        mySpecies[s1].heatTq = heatTq[s2];
        mySpecies[s1].heatSumT = heatSumT[s2];                //temperature sum for recovery - sum of (25-mean)
        mySpecies[s1].heatRecoverT = heatRecoverT[s2];
        mySpecies[s1].usingColdStress = useColdStress[s2].ToLower() == "yes";
        mySpecies[s1].coldOnsetT = coldOnsetT[s2];           //onset tempeature for cold effects
        mySpecies[s1].coldFullT = coldFullT[s2];            //full tempeature for cold effects
        mySpecies[s1].coldTq = coldTq[s2];
        mySpecies[s1].coldSumT = coldSumT[s2];                //temperature sum for recovery - sum of means
        mySpecies[s1].coldRecoverT = coldRecoverT[s2];

        // CO2 effects
        mySpecies[s1].referenceCO2 = referenceCO2[s2];
        mySpecies[s1].CO2PmaxScale = CO2PmaxScale[s2];
        mySpecies[s1].CO2NScale = CO2NScale[s2];
        mySpecies[s1].CO2NMin = CO2NMin[s2];
        mySpecies[s1].CO2NCurvature = CO2NCurvature[s2];

        ////  >> Parition of new growth  >>>
        mySpecies[s1].maxRootFraction = maxRootFraction[s2];
        mySpecies[s1].targetSRratio = targetSRratio[s2];
        mySpecies[s1].allocationSeasonF = allocationSeasonF[s2];
        mySpecies[s1].startHighAllocation = StartHighAllocation[s2];
        mySpecies[s1].durationHighAllocation = DurationHighAllocation[s2];
        mySpecies[s1].shoulderHighAllocation = ShoulderHighAllocation[s2];
        mySpecies[s1].usingLatFunctionFShoot = useLatitudeFunction[s2].ToLower() == "yes";
        mySpecies[s1].referenceLatitude = ReferenceLatitude[s2];
        mySpecies[s1].paramALatFunction = paramALatFunction[s2];
        mySpecies[s1].onsetFacLatFunction = onsetFacLatFunction[s2];
        mySpecies[s1].outsetFacLatFunction = outsetFacLatFunction[s2];
        mySpecies[s1].maxShoulderLatFunction = maxShoulderLatFunction[s2];
        mySpecies[s1].minPlateauLatFunction = minPlateauLatFunction[s2];
        mySpecies[s1].paramBLatFunction = paramBLatFunction[s2];
        mySpecies[s1].allocationMax = allocationMax[s2];
        mySpecies[s1].paramCLatFunction = paramCLatFunction[s2];

        mySpecies[s1].maxFLeaf = maxFLeaf[s2];
        mySpecies[s1].minFLeaf = minFLeaf[s2];
        mySpecies[s1].dmMaxFLeaf = dmMaxFLeaf[s2];
        mySpecies[s1].dmReferenceFLeaf = dmReferenceFLeaf[s2];
        mySpecies[s1].exponentFLeaf = exponentFLeaf[s2];

        mySpecies[s1].fStolon = fStolon[s2];

        mySpecies[s1].specificLeafArea = specificLeafArea[s2];
        mySpecies[s1].specificRootLength = specificRootLength[s2];

        ////  >> Tissue turnover and senescence  >>>
        mySpecies[s1].liveLeavesPerTiller = liveLeavesPerTiller[s2];

        mySpecies[s1].refTissueTurnoverRate = rateLive2Dead[s2];
        mySpecies[s1].facGrowingTissue = facGrowingTissue[s2];
        mySpecies[s1].refTurnoverRateStolon = refTurnoverRateStolon[s2];
        mySpecies[s1].refLitteringRate = rateDead2Litter[s2];
        mySpecies[s1].rateRootSen = rateRootSen[s2];
        mySpecies[s1].massFluxTmin = massFluxTmin[s2];
        mySpecies[s1].massFluxTopt = massFluxTopt[s2];
        mySpecies[s1].massFluxTq = massFluxTq[s2];
        mySpecies[s1].massFluxW0 = massFluxW0[s2];
        mySpecies[s1].massFluxWopt = massFluxWopt[s2];
        mySpecies[s1].exponentGLFW2dead = massFluxDeadWq[s2];
        mySpecies[s1].stockParameter = stockParameter[s2];
        mySpecies[s1].Kappa2 = Kappa2_Remob[s2];
        mySpecies[s1].Kappa3 = Kappa3_Remob[s2];
        mySpecies[s1].Kappa4 = Kappa4_Remob[s2];

        ////  >> Digestibility and feed quality  >>>
        mySpecies[s1].digestLive = digestLive[s2];
        mySpecies[s1].digestDead = digestDead[s2];

        ////  >> DM limits for harvest and senescence  >>>
        mySpecies[s1].dmgreenmin = dmgreenmin[s2];
        mySpecies[s1].dmdeadmin = dmdeadmin[s2];

        ////  >> N fixation  >>>
        mySpecies[s1].MaxFix = NMaxFix[s2];   //N-fix fraction when no soil N available, read in later
        mySpecies[s1].MinFix = NMinFix[s2];   //N-fix fraction when soil N sufficient

        ////  >> Growth limiting factor  >>>
        mySpecies[s1].NdilutCoeff = NdilutCoeff[s2];
        mySpecies[s1].waterStressFactor = waterStressFactor[s2];
        mySpecies[s1].soilSatFactor = soilSatFactor[s2];
        mySpecies[s1].GLFSFertility = SFertilityGLF[s2];
        mySpecies[s1].GLFGeneric = GenericGLF[s2];

        ////  >> Root depth and distribution  >>>
        mySpecies[s1].usingSpeciesRoot = usingSpeciesRoot;
        if (usingSpeciesRoot)
        { // root specified for each species
            mySpecies[s1].rootDistributionMethod = p_RootDistributionMethod;
            mySpecies[s1].expoLinearDepthParam = p_ExpoLinearDepthParam[s2];
            mySpecies[s1].expoLinearCurveParam = p_ExpoLinearCurveParam[s2];
            mySpecies[s1].MaximumUptakeRateNH4 = MaximumUptakeRateNH4[s2];
            mySpecies[s1].MaximumUptakeRateNO3 = MaximumUptakeRateNO3[s2];
            mySpecies[s1].referenceRLD = referenceRLD[s2];
            mySpecies[s1].NextraSWF = NextraSWF[s2];
        }
        else
        { // root specified for whole sward (use first species as data entry)
            mySpecies[s1].rootDistributionMethod = p_RootDistributionMethod;
            mySpecies[s1].expoLinearDepthParam = p_ExpoLinearDepthParam[0];
            mySpecies[s1].expoLinearCurveParam = p_ExpoLinearCurveParam[0];
            mySpecies[s1].MaximumUptakeRateNH4 = MaximumUptakeRateNH4[0];
            mySpecies[s1].MaximumUptakeRateNO3 = MaximumUptakeRateNO3[0];
            mySpecies[s1].referenceRLD = referenceRLD[0];
            mySpecies[s1].NextraSWF = NextraSWF[0];
        }

        ////  >> Plant height  >>>
        mySpecies[s1].usingSpeciesHeight = usingSpeciesHeight;
        mySpecies[s1].MaxPlantHeight = MaxPlantHeight[s2];
        mySpecies[s1].MassForMaxHeight = MassForMaxHeight[s2];
        mySpecies[s1].ExponentHeightFromMass = ExponentHeightFromMass[s2];
        mySpecies[s1].MinimumHeight = MinimumHeight;

        //// N concentrations ............................................
        mySpecies[s1].NcstemFr = RelativeNconc_Stems[s2];
        mySpecies[s1].NcstolFr = RelativeNconc_Stolons[s2];
        mySpecies[s1].NcrootFr = RelativeNconc_Roots[s2];

        mySpecies[s1].NcRel2 = RelativeNconc_stage2[s2];
        mySpecies[s1].NcRel3 = RelativeNconc_stage3[s2];

        // Note: 0.01 is for conversion of % to fraction
        mySpecies[s1].NcleafOpt = 0.01 * NconcOptimum_leaves[s2];
        mySpecies[s1].NcstemOpt = mySpecies[s1].NcleafOpt * mySpecies[s1].NcstemFr;
        mySpecies[s1].NcstolOpt = mySpecies[s1].NcleafOpt * mySpecies[s1].NcstolFr;
        mySpecies[s1].NcrootOpt = mySpecies[s1].NcleafOpt * mySpecies[s1].NcrootFr;

        mySpecies[s1].NcleafMax = 0.01 * NconcMaximum_leaves[s2];
        mySpecies[s1].NcstemMax = mySpecies[s1].NcleafMax * mySpecies[s1].NcstemFr;
        mySpecies[s1].NcstolMax = mySpecies[s1].NcleafMax * mySpecies[s1].NcstolFr;
        mySpecies[s1].NcrootMax = mySpecies[s1].NcleafMax * mySpecies[s1].NcrootFr;

        mySpecies[s1].NcleafMin = 0.01 * NconcMinimum_leaves[s2];
        mySpecies[s1].NcstemMin = mySpecies[s1].NcleafMin * mySpecies[s1].NcstemFr;
        mySpecies[s1].NcstolMin = mySpecies[s1].NcleafMin * mySpecies[s1].NcstolFr;
        mySpecies[s1].NcrootMin = mySpecies[s1].NcleafMin * mySpecies[s1].NcrootFr;

        // initial N equals optimum, except tissue4, equal to minimum
        mySpecies[s1].Ncleaf1 = mySpecies[s1].NcleafOpt;
        mySpecies[s1].Ncleaf2 = mySpecies[s1].NcleafOpt * mySpecies[s1].NcRel2;
        mySpecies[s1].Ncleaf3 = mySpecies[s1].NcleafOpt * mySpecies[s1].NcRel3;
        mySpecies[s1].Ncleaf4 = mySpecies[s1].NcleafMin;

        mySpecies[s1].Ncstem1 = mySpecies[s1].NcstemOpt;
        mySpecies[s1].Ncstem2 = mySpecies[s1].NcstemOpt * mySpecies[s1].NcRel2;
        mySpecies[s1].Ncstem3 = mySpecies[s1].NcstemOpt * mySpecies[s1].NcRel3;
        mySpecies[s1].Ncstem4 = mySpecies[s1].NcstemMin;

        mySpecies[s1].Ncstol1 = mySpecies[s1].NcstolOpt;
        mySpecies[s1].Ncstol2 = mySpecies[s1].NcstolOpt * mySpecies[s1].NcRel2;
        mySpecies[s1].Ncstol3 = mySpecies[s1].NcstolOpt * mySpecies[s1].NcRel3;

        mySpecies[s1].Ncroot = mySpecies[s1].NcrootOpt;

        //// Additional initialisation bits ..............................
        mySpecies[s1].fShoot = 1;            // actual fraction of dGrowth allocated to shoot

        int nLayers = dlayer.Length;
        soilAvailableWater = new double[nLayers];
        soilWaterUptake = new double[nLayers];
        soilNH4Available = new double[nLayers];
        soilNH4Uptake = new double[nLayers];
        soilNO3Available = new double[nLayers];
        soilNO3Uptake = new double[nLayers];

        mySpecies[s1].soilAvailableWater = new double[nLayers];
        mySpecies[s1].soilAvailableNH4 = new double[nLayers];
        mySpecies[s1].soilAvailableNO3 = new double[nLayers];
    }

    /// <summary>
    /// Set DM and N values for each species in the sward
    /// </summary>
    /// <param name="s">The index for the species being setup</param>
    private void SetSpeciesState(int s, SpeciesStateSettings MyState)
    {
        double iniDMshoot = mySpecies[s].dmshoot;
        double iniDMroot = mySpecies[s].dmroot;
        double iniNshot = mySpecies[s].Nshoot;
        double iniNroot = mySpecies[s].Nroot;

        //// Shoot DM ....................................................
        mySpecies[s].dmshoot = MyState.ShootDM;

        mySpecies[s].phenoStage = 0;
        if (mySpecies[s].dmshoot > 0.0)
            mySpecies[s].phenoStage = 1;

        mySpecies[s].dmleaf1 = mySpecies[s].dmshoot * MyState.DMFraction[0];
        mySpecies[s].dmleaf2 = mySpecies[s].dmshoot * MyState.DMFraction[1];
        mySpecies[s].dmleaf3 = mySpecies[s].dmshoot * MyState.DMFraction[2];
        mySpecies[s].dmleaf4 = mySpecies[s].dmshoot * MyState.DMFraction[3];
        mySpecies[s].dmstem1 = mySpecies[s].dmshoot * MyState.DMFraction[4];
        mySpecies[s].dmstem2 = mySpecies[s].dmshoot * MyState.DMFraction[5];
        mySpecies[s].dmstem3 = mySpecies[s].dmshoot * MyState.DMFraction[6];
        mySpecies[s].dmstem4 = mySpecies[s].dmshoot * MyState.DMFraction[7];
        mySpecies[s].dmstol1 = mySpecies[s].dmshoot * MyState.DMFraction[8];
        mySpecies[s].dmstol2 = mySpecies[s].dmshoot * MyState.DMFraction[9];
        mySpecies[s].dmstol3 = mySpecies[s].dmshoot * MyState.DMFraction[10];

        //// Root DM  ....................................................
        mySpecies[s].dmroot = MyState.RootDM;
        mySpecies[s].rootDepth = MyState.RootDepth;

        //// Initial N amount in each pool ...............................
        mySpecies[s].Nleaf1 = mySpecies[s].dmleaf1 * MyState.NConcentration[0];
        mySpecies[s].Nleaf2 = mySpecies[s].dmleaf2 * MyState.NConcentration[1];
        mySpecies[s].Nleaf3 = mySpecies[s].dmleaf3 * MyState.NConcentration[2];
        mySpecies[s].Nleaf4 = mySpecies[s].dmleaf4 * MyState.NConcentration[3];
        mySpecies[s].Nstem1 = mySpecies[s].dmstem1 * MyState.NConcentration[4];
        mySpecies[s].Nstem2 = mySpecies[s].dmstem2 * MyState.NConcentration[5];
        mySpecies[s].Nstem3 = mySpecies[s].dmstem3 * MyState.NConcentration[6];
        mySpecies[s].Nstem4 = mySpecies[s].dmstem4 * MyState.NConcentration[7];
        mySpecies[s].Nstol1 = mySpecies[s].dmstol1 * MyState.NConcentration[8];
        mySpecies[s].Nstol2 = mySpecies[s].dmstol2 * MyState.NConcentration[9];
        mySpecies[s].Nstol3 = mySpecies[s].dmstol3 * MyState.NConcentration[10];
        mySpecies[s].Nroot = mySpecies[s].dmroot * MyState.NConcentration[11];

        //// Aggregated DM variables .....................................
        mySpecies[s].UpdateAggregated();

        //// Plant height ................................................
        if (usingSpeciesHeight)
        { // each species has it own height
            mySpecies[s].height = mySpecies[s].HeightfromDM();
        }

        //// Root depth and distribution  ................................
        mySpecies[s].rootDepth = MyState.RootDepth;
        if (usingSpeciesRoot)
        { // each species has it own root distribution
            mySpecies[s].rootFraction = RootProfileDistribution(s);
        }

        //// LAI ........................................................
        mySpecies[s].greenLAI = mySpecies[s].GreenLAI();
        mySpecies[s].deadLAI = mySpecies[s].DeadLAI();
        mySpecies[s].totalLAI = mySpecies[s].greenLAI + mySpecies[s].deadLAI;

        double endDMshoot = mySpecies[s].dmshoot;
        double endDMroot = mySpecies[s].dmroot;
        double endNshot = mySpecies[s].Nshoot;
        double endNroot = mySpecies[s].Nroot;
    }

    /// <summary>
    /// Let other module (micromet and SWIM) know about the existence of this crop (sward)
    /// </summary>
    /// <remarks>
    ///  Ideally we should advertise each species, other module would do the resource arbitration.
    ///  However, when doing this we have to supply some data each for each module: the events 
    ///  'New_Canopy' and 'NewPotentialGrowth' are used by micromet, but for SWIM outputs are needed
    ///  (RLV and WaterDemand).  Raising events for each species work fine, but to have outputs sepately
    ///  for each species would require changing the status of species (It would need to be a module on
    ///  its own right - SWIM registers each crop module and then will as for the variables as it needs)
    ///  This is possible, but require time and will be left as it is for now. The resource arbitration
    ///  has to be done within AgPasture (RCichota, Nov2014)
    /// </remarks>
    private void AdvertiseThisCrop()
    {
        NewCropType cropData = new NewCropType();

        cropData.crop_type = micrometType[0];
        cropData.sender = thisCropName;
        NewCrop.Invoke(cropData);
    }

    /// <summary>
    /// Write initialisation info to summary file
    /// </summary>
    private void writeSummary()
    {
        Console.WriteLine();
        Console.Write(@"
           AgPature Properties
         -----------------------------------------------------------------------------
          Species        TotalWt  ShootWt  RootWt   LAI  TotalC   TotalN   RootDepth
                         (kg/ha)  (kg/ha)  (kg/ha)   () (kg/ha)   (kg/ha)       (mm)
         -----------------------------------------------------------------------------
");
        for (int specie = 0; specie < mySpecies.Length; ++specie)
        {
            Console.WriteLine("          {0,-12}    {1,6:F1}   {2,6:F1}  {3,6:F1}  {4,4:F2}  {5,6:F1}    {6,5:F1}      {7,6:F1}",
            mySpecies[specie].speciesName,
            mySpecies[specie].dmshoot + mySpecies[specie].dmroot,
            mySpecies[specie].dmshoot,
            mySpecies[specie].dmroot,
            mySpecies[specie].totalLAI,
            (mySpecies[specie].dmshoot + mySpecies[specie].dmroot) * 0.4,
            mySpecies[specie].Nshoot + mySpecies[specie].Nroot,
            mySpecies[specie].rootDepth);
        }

        Console.WriteLine("         -----------------------------------------------------------------------------");
        Console.WriteLine("          Totals          {0,6:F1}   {1,6:F1}  {2,6:F1}  {3,4:F2}  {4,6:F1}    {5,5:F1}      {6,6:F1}",
        TotalPlantWt, AboveGroundWt, BelowGroundWt, LAI_total, TotalPlantC, TotalPlantN, swardRootDepth);
        Console.WriteLine("         -----------------------------------------------------------------------------");

        Console.WriteLine();
        Console.WriteLine("          - N uptake controlled by " + ((NUptakeSource == "calc") ? "AgPasture" : "an external module"));
        Console.WriteLine("          - Water uptake controlled by " + ((WaterUptakeSource == "calc") ? "AgPasture" : "an external module"));
        Console.WriteLine();

        Console.Write(@"
          Root distribution
         -----------------------------
          Layer     Depth  FractionWt
         -----------------------------
");
        double LayerTop = 0;
        for (int layer = 0; layer < dlayer.Length; layer++)
        {
            Console.WriteLine("          {0,3}  {1,10}     {2,6:F3}", layer, LayerTop.ToString() + "-" + (LayerTop + dlayer[layer]).ToString(), RootFraction[layer]);
            LayerTop += dlayer[layer];
        }

        Console.WriteLine("         -----------------------------");
    }

    #endregion  -----------------------------------------------------------------------------------

    # region Main daily processes  ----------------------------------------------------------------

    /// <summary>
    /// EventHandler - get new met data (not really used)
    /// </summary>
    /// <param name="NewMetData"></param>
    [EventHandler]
    public void OnNewMet(NewMetType NewMetData)
    {
        for (int s = 0; s < NumSpecies; s++)
            mySpecies[s].DailyRefresh();
    }

    /// <summary>
    /// EventHandler - preparation before the main process
    /// </summary>
    [EventHandler]
    public void OnPrepare()
    {
        // RCichota May2014, moved here from onProcess (really ought to be onNewMet but have issues at initialisation)
        //**Zero out some variables
        //for (int s = 0; s < NumSpecies; s++)
        //    mySpecies[s].DailyRefresh();

        // Clear FractionHarvest array
        Array.Clear(FractionToHarvest, 0, FractionToHarvest.Length);

        // Get sward average plant height
        if (usingSpeciesHeight)
        {
            swardHeight = mySpecies[0].height * mySpecies[0].dmshoot;
            for (int s = 1; s < NumSpecies; s++)
            {
                swardHeight += mySpecies[s].height * mySpecies[s].dmshoot;
            }
            swardHeight /= AboveGroundWt;
        }
        else
        {
            swardHeight = HeightfromDM();
            for (int s = 0; s < NumSpecies; s++)
            {
                mySpecies[s].height = swardHeight;
            }
        }

        // Get sward average root distribution
        if (usingSpeciesRoot)
        {
            int nLayers = dlayer.Length;
            RootFraction = new double[nLayers];
            for (int layer = 0; layer < nLayers; layer++)
            {
                for (int s = 0; s < NumSpecies; s++)
                {
                    RootFraction[layer] += mySpecies[s].dmroot * mySpecies[s].rootFraction[layer];
                }
                RootFraction[layer] /= RootWt;
            }
        }
        //else  root distribution does not change 

        // Send info about canopy and potential growth, used by other modules to calculate intercepted radn and ET
        DoNewCanopyEvent();
        DoNewPotentialGrowthEvent();
    }

    /// <summary>
    /// Perform the main process phase
    /// </summary>
    [EventHandler]
    public void OnProcess()
    {
        if (!isAlive)
            return;

        // Remember last status, and update root depth frontier (root depth for annuals)
        for (int s = 0; s < NumSpecies; s++)
        {
            mySpecies[s].SetPrevPools();

            double newRootDepth = mySpecies[s].rootGrowth();
            if (swardRootDepth < newRootDepth)
            { // the deepest root_depth is used
                swardRootDepth = newRootDepth;
                swardRootZoneBottomLayer = mySpecies[s].RootZoneBottomLayer();
            }
        }

        // Pass on some parameters to different species
        SetSpeciesWithSwardData();

        // Allocate resources for each species
        PartitionAboveGroundResources();

        // Phenology, for annuals
        int anyEmerged = 0;
        for (int s = 0; s < NumSpecies; s++)
        {
            anyEmerged += mySpecies[s].Phenology();
        }

        // Get potential growth
        swardPotentialGrowth = 0;
        for (int s = 0; s < NumSpecies; s++)
        {
            if (usingSpeciesPhotosynthesis)
            {
                mySpecies[s].DailyPotentialPhotosynthesis();
                mySpecies[s].DailyPlantRespiration();
                swardPotentialGrowth += mySpecies[s].DailyPotentialGrowth();
            }
            else
            {
                swardPotentialGrowth += mySpecies[s].DailyGrowthPot();
            }
        }

        // Get soil water available in the root zone
        soilAvailableWater = PlantWaterAvailability();


        // Evaluate the water supply, demand & uptake
        WaterBudgetAndUptake();


        // Calculate and set the growth limiting factor due to soil moisture
        SetSpeciesGLFWater();

        // Consider water effects (before considering other nutrient limitation)
        swardPotGrowthWater = 0.0;
        for (int s = 0; s < NumSpecies; s++)
        {
            swardPotGrowthWater += mySpecies[s].DailyGrowthW();
        }

        // Get soil N available in the root zone
        swardSoilNavailable = PlantNExtractability();

        // Compute the N budget and uptake
        NBudgetAndUptake();

        // Calculate and set the growth limiting factor due to nitrogen
        SetSpeciesGLFNitrogen();

        // Compute the actual daily growth
        swardActualGrowth = 0.0;
        for (int s = 0; s < NumSpecies; s++)
        {
            swardActualGrowth += mySpecies[s].DailyGrowthAct();
        }

        // DM partitioning & tissue turnover
        GrowthAndPartition();

        // Do the actual uptake (send changes to other modules)
        UptakeWaterAndN();
    }

    /// <summary>
    /// Send out info about canopy
    /// </summary>
    /// <remarks>
    ///  - micromet uses to compute radiation interception and ET
    ///  Ideally we should pass the values for each species, micromet would then do the resource arbitration.
    ///  However, this is not possible due to comflict with SWIM (see coment on AdvertiseThisCrop())
    ///  </remarks>
    private void DoNewCanopyEvent()
    {
        NewCanopyType canopyData = new NewCanopyType();

        //  Pack and send info about the average sward canopy
        canopyData.sender = thisCropName;
        canopyData.lai = (float)swardGreenLAI;
        canopyData.lai_tot = (float)swardTotalLAI;
        canopyData.height = (int)swardHeight;
        canopyData.depth = (int)swardHeight;
        canopyData.cover = (float)Cover_green;
        canopyData.cover_tot = (float)Cover_tot;

        New_Canopy.Invoke(canopyData);
    }

    /// <summary>
    /// Send out info about potential limitation to growth
    /// </summary>
    /// <remarks>
    ///  - micromet uses this to compute radiation interception and ET
    ///  Ideally we should pass the values for each species, micromet would then do the resource arbitration.
    ///  However, this is not possible due to comflict with SWIM (see coment on AdvertiseThisCrop())
    /// </remarks>
    private void DoNewPotentialGrowthEvent()
    {
        double Tday = (0.75 * MetData.MaxT)
                    + (0.25 * MetData.MinT);
        swardGLFTemp = 0.0;     // this will be the glfTemp output, as weighted average
        for (int s = 0; s < NumSpecies; s++)
        {
            double prop = 1.0 / NumSpecies;
            if (swardGreenDM != 0.0)
            {
                prop = MathUtility.Divide(mySpecies[s].dmgreen, swardGreenDM, 1.0);
            }

            swardGLFTemp += mySpecies[s].GFTemperature(Tday) * prop;
        }

        double gft = 1;
        if (Tday < 20)
        {
            gft = Math.Sqrt(swardGLFTemp);
        }
        else
        {
            gft = swardGLFTemp;
        }
        // Note: swardGLFTemp is for gross photosysthesis.
        // This is different from that for net production as used in other APSIM crop models, and is
        // assumed in calculation of temperature effect on transpiration (in micromet).
        // Here we passed it as sqrt - (Doing so by a comparison of swardGLFTemp and that
        // used in wheat). Temperature effects on NET production of forage species in other models
        // (e.g., grassgro) are not so significant for T = 10-20 degrees(C)

        frgr = Math.Min(FVPD, gft);
        frgr = Math.Min(frgr, GLFgeneric);

        // Pack and send the information
        NewPotentialGrowthType PGrowthData = new NewPotentialGrowthType();
        PGrowthData.sender = thisCropName;
        PGrowthData.frgr = (float)frgr;

        NewPotentialGrowth.Invoke(PGrowthData);
    }

    /// <summary>
    /// Get plant potential transpiration (from micromet)
    /// </summary>
    /// <param name="waterDemandData">plant water demand</param>
    [EventHandler]
    public void OnCanopy_Water_Balance(CanopyWaterBalanceType waterDemandData)
    {
        swardWaterDemand = 0.0;
        for (int i = 0; i < waterDemandData.Canopy.Length; i++)
        {
            if (waterDemandData.Canopy[i].name.ToUpper() == thisCropName.ToUpper())
            {
                swardWaterDemand = waterDemandData.Canopy[i].PotentialEp;
                // Note: water demand for whole sward, partition done later
            }
        }
    }

    /// <summary>
    /// Get light interception data (energy balance)
    /// </summary>
    /// <param name="lightInterceptionData">light interception data</param>
    [EventHandler]
    public void OnCanopy_Energy_Balance(CanopyEnergyBalanceType lightInterceptionData)
    {
        InterceptedRadn = 0.0;
        for (int i = 0; i < lightInterceptionData.Interception.Length; i++)
        {
            if (lightInterceptionData.Interception[i].name.ToUpper() == thisCropName.ToUpper())
            {
                for (int j = 0; j < lightInterceptionData.Interception[i].layer.Length; j++)
                {
                    InterceptedRadn += lightInterceptionData.Interception[i].layer[j].amount;
                    // light interception considered for whole sward, partition between species done later
                }
            }
        }
    }

    /// <summary>
    /// Respond to a WaterUptakesCalculated event
    /// </summary>
    /// <param name="SoilWater">WaterUptakesCalculated</param>
    [EventHandler]
    public void OnWaterUptakesCalculated(WaterUptakesCalculatedType SoilWater)
    {
        // Gets the water uptake for each layer as calculated by an external module (SWIM)
        for (int i_Crop = 0; i_Crop != SoilWater.Uptakes.Length; i_Crop++)
        {
            string MyName = SoilWater.Uptakes[i_Crop].Name;
            if (MyName == thisCropName)
            {
                swardWaterUptakeByAPSIM = new double[dlayer.Length];
                int length = SoilWater.Uptakes[i_Crop].Amount.Length;
                for (int layer = 0; layer < length; layer++)
                {
                    swardWaterUptakeByAPSIM[layer] = SoilWater.Uptakes[i_Crop].Amount[layer];
                }
            }
        }
    }

    /// <summary>
    /// Respond to a NUptakesCalculated event
    /// </summary>
    /// <param name="SoilNData">NUptakesCalculated data</param>
    [EventHandler]
    public void OnNUptakesCalculated(NUptakesCalculatedType SoilNData)
    {
        // Gets the water uptake for each layer as calculated by an external module (SWIM)
        for (int i_Crop = 0; i_Crop != SoilNData.Uptakes.Length; i_Crop++)
        {
            string MyName = SoilNData.Uptakes[i_Crop].Name;
            if (MyName == thisCropName)
            {
                swardNH4UptakeByAPSIM = new double[dlayer.Length];
                swardNO3UptakeByAPSIM = new double[dlayer.Length];
                for (int layer = 0; layer < SoilNData.Uptakes[i_Crop].NH4Amount.Length; layer++)
                    swardNH4UptakeByAPSIM[layer] = SoilNData.Uptakes[i_Crop].NH4Amount[layer];
                for (int layer = 0; layer < SoilNData.Uptakes[i_Crop].NO3Amount.Length; layer++)
                    swardNO3UptakeByAPSIM[layer] = SoilNData.Uptakes[i_Crop].NO3Amount[layer];
            }
        }
    }

    /// <summary>
    /// Let species know the value of some sward variables
    /// </summary>
    private void SetSpeciesWithSwardData()
    {
        //// pass CO2 & canopy to species
        Species.swardInterceptedRadn = InterceptedRadn;
        Species.swardCoverGreen = Cover_green;
        Species.swardLightExtCoeff = swardLightExtCoeff;

        // pass on values for dlayer
        for (int s = 0; s < NumSpecies; s++)
        {
            mySpecies[s].dlayer = dlayer;
        }
    }

    /// <summary>
    /// Estimate the allocation of intercepted radiation and ET for each species
    /// </summary>
    private void PartitionAboveGroundResources()
    {
        // Intercepted solar Radn and ET were considered for whole sward, partition between species based on LAI and lightExtCoeff
        double sumkLAI = 0.0;
        for (int s = 0; s < NumSpecies; s++)
        {
            sumkLAI += mySpecies[s].greenLAI * mySpecies[s].lightExtCoeff;
        }

        for (int s = 0; s < NumSpecies; s++)
        {
            if (sumkLAI == 0.0)
            {
                mySpecies[s].intRadnFrac = 0.0;
                mySpecies[s].interceptedRadn = 0.0;
            }
            else
            {
                mySpecies[s].intRadnFrac = mySpecies[s].greenLAI * mySpecies[s].lightExtCoeff / sumkLAI;
                mySpecies[s].interceptedRadn = InterceptedRadn * mySpecies[s].intRadnFrac;

                mySpecies[s].WaterDemand = swardWaterDemand * mySpecies[s].intRadnFrac;
            }
        }
    }

    /// <summary>
    /// Get the amount of plant available water in the soil
    /// </summary>
    /// <returns>Amount of plant available soil water</returns>
    private double[] PlantWaterAvailability()
    {
        // clear some variables
        for (int s = 0; s < NumSpecies; s++)
            Array.Clear(mySpecies[s].soilAvailableWater, 0, dlayer.Length);

        if (WaterUptakeSource.ToLower() == "calc")
        {
            if (usingAlternativeWAvailability)
            {
                return PlantAvailableSoilWater();
            }
            else
            {
                return PlantAvailableWaterClassic();
            }
        }
        else
        {
            return PlantAvailableWaterAPSIM();
        }
    }

    /// <summary>
    /// Get the amount of plant available soil water
    /// This method consider root presence in each layer and classic definition of kl,
    /// i.e., kl is the fraction of water available for uptake
    /// </summary>
    /// <returns>Amount of plant available water</returns>
    private double[] PlantAvailableWaterClassic()
    {
        // zero out the variables
        double[] PAW = new double[dlayer.Length];
        double layerFrac = 0.0;              // fraction of layer explored by roots
        double PotAvailableWater = 0.0;      // total potential PAW (or maximum)
        double wFrac = 0.0;                  // availability faction for each species

        // find out plant soil available water
        if (usingWAvailableBySpecies)
        { // considering root presence for each species
            for (int s = 0; s < NumSpecies; s++)
            { // amount if each species was alone
                for (int layer = 0; layer <= mySpecies[s].RootZoneBottomLayer(); layer++)
                {
                    layerFrac = LayerFractionForRoots(layer, mySpecies[s].rootDepth);
                    PotAvailableWater = sw_dep[layer] - (ll[layer] * dlayer[layer]);
                    PotAvailableWater *= layerFrac;
                    mySpecies[s].soilAvailableWater[layer] = Math.Max(0.0, kl[layer] * PotAvailableWater);
                }
            }

            // Adjust the values so that total available water is equal or less than maximum
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                // maximum PAW for this layer
                PotAvailableWater = sw_dep[layer] - (ll[layer] * dlayer[layer]);
                // adjust factor
                wFrac = MathUtility.Divide(PotAvailableWater, mySpecies.Sum(x => x.soilAvailableWater[layer]), 0.0);
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].soilAvailableWater[layer] *= Math.Min(1.0, wFrac);
                    PAW[layer] += mySpecies[s].soilAvailableWater[layer];
                }
            }
        }
        else
        {  // considering the whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                PotAvailableWater = sw_dep[layer] - (ll[layer] * dlayer[layer]);
                PotAvailableWater *= layerFrac;
                PAW[layer] = Math.Max(0.0, kl[layer] * PotAvailableWater);
                for (int s = 0; s < NumSpecies; s++)
                { // partition based on demand
                    wFrac = MathUtility.Divide(mySpecies[s].WaterDemand, swardWaterDemand, 0.0);
                    mySpecies[s].soilAvailableWater[layer] = PAW[layer] * Math.Min(1.0, wFrac);
                }
            }
        }

        return PAW;
    }

    /// <summary>
    /// Get the amount of plant available soil water
    /// This method consider root distribution (density) plus a new definition of kl,
    /// i.e., kl is a factor describing how easy is to take up water (roughly related to soil conductivity)
    /// </summary>
    /// <returns>Amount of plant available water</returns>
    private double[] PlantAvailableSoilWater()
    {
        double[] PAW = new double[dlayer.Length];
        double layerFrac = 0.0;              // fraction of layer explored by roots
        double PotAvailableWater = 0.0;      // total potential PAW (or maximum)
        double xFac = 1.0;                   // extractability factor for each layer
        double wFrac = 0.0;                  // availability faction for each species

        // find out soil available water
        if (usingWAvailableBySpecies)
        {  // Considering each species
            for (int s = 0; s < NumSpecies; s++)
            { // amount if each species was alone
                for (int layer = 0; layer <= mySpecies[s].RootZoneBottomLayer(); layer++)
                {
                    layerFrac = LayerFractionForRoots(layer, mySpecies[s].rootDepth);
                    PotAvailableWater = sw_dep[layer] - (ll[layer] * dlayer[layer]);
                    PotAvailableWater = Math.Max(0.0, PotAvailableWater * layerFrac);
                    if (mySpecies[s].referenceRLD > 0.0)
                    {
                        xFac = Math.Min(1.0, kl[layer] * mySpecies[s].RLD[layer] / mySpecies[s].referenceRLD);
                    }
                    else
                    {
                        xFac = Math.Min(1.0, kl[layer]);
                    }
                    mySpecies[s].soilAvailableWater[layer] = PotAvailableWater * xFac;
                }
            }

            // Adjust the values so that total available water is equal or less than maximum
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                // maximum PAW for this layer
                PotAvailableWater = sw_dep[layer] - (ll[layer] * dlayer[layer]);
                // adjust paw for each species
                wFrac = MathUtility.Divide(PotAvailableWater, mySpecies.Sum(x => x.soilAvailableWater[layer]), 0.0);
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].soilAvailableWater[layer] *= Math.Min(1.0, wFrac);
                    PAW[layer] += mySpecies[s].soilAvailableWater[layer];
                }
            }
        }
        else
        {  // considering whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                PotAvailableWater = sw_dep[layer] - (ll[layer] * dlayer[layer]);
                PotAvailableWater = Math.Max(0.0, PotAvailableWater * layerFrac);
                if (referenceRLD[0] > 0.0)
                {
                    xFac = Math.Min(1.0, kl[layer] * rlv[layer] / referenceRLD[0]);
                }
                else
                {
                    xFac = Math.Min(1.0, kl[layer]);
                }

                PAW[layer] = PotAvailableWater * xFac;
                for (int s = 0; s < NumSpecies; s++)
                { // partition based on root distribution
                    wFrac = MathUtility.Divide(mySpecies[s].RLD[layer], rlv[layer], 0.0);
                    mySpecies[s].soilAvailableWater[layer] = PAW[layer] * Math.Min(1.0, wFrac);
                }
            }
        }

        return PAW;
    }

    /// <summary>
    /// Get the amount of plant available soil water
    /// This method provides only an estimated partition, the actual uptake was already computed
    /// by other module (SWIM)
    /// </summary>
    /// <returns>Amount of plant available water</returns>
    private double[] PlantAvailableWaterAPSIM()
    {
        double[] PAW = new double[dlayer.Length];
        double wFrac = 0.0;                  // availability faction for each species

        // check that we have an input from apsim
        if (swardWaterUptakeByAPSIM == null)
            throw new Exception("No module provided an estimate for water uptake, check water module or set WaterUptakeSource to \"calc\"");

        // find out soil available water
        if (usingAlternativeWUptake)
        { // verify that available water comes from where roots are, for each species

            double[] totAuxAvail = new double[swardRootZoneBottomLayer + 1];
            double[] totAllowable = new double[NumSpecies];
            double[] auxAllowable = new double[NumSpecies];
            double[] auxFac = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
            {
                for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                {
                    if (mySpecies[s].rootFraction[layer] > 0.0)
                    {
                        mySpecies[s].soilAvailableWater[layer] = swardWaterUptakeByAPSIM[layer];
                        if (mySpecies[s].WaterDemand > 0.0)
                            totAuxAvail[layer] = swardWaterUptakeByAPSIM[layer];
                    }
                }

                totAllowable[s] = mySpecies[s].soilAvailableWater.Sum();
                auxAllowable[s] = Math.Min(mySpecies[s].WaterDemand, totAllowable[s]);
                auxFac[s] = MathUtility.Divide(mySpecies[s].WaterDemand, totAllowable[s], double.MaxValue);
            }

            double totUptake = Math.Min(totAuxAvail.Sum(), auxAllowable.Sum());
            double auxBal = MathUtility.Divide(totUptake - auxAllowable.Sum(), auxAllowable.Sum(), 0.0);
            double[] FacBal = new double[NumSpecies];
            double[] auxMulti = new double[NumSpecies];

            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                wFrac = MathUtility.Divide(totUptake, totAuxAvail.Sum(), 0.0);
                PAW[layer] = totAuxAvail[layer] * Math.Min(1.0, wFrac);
                //// Note: because root presence is considered, PAW might be smaller than swardWaterUptakeByAPSIM, this has to be
                ////  given back to swim or mass imbalance will happen (this is done when Agpasture considers uptake)
            }

            for (int s = 0; s < NumSpecies; s++)
            {
                FacBal[s] = (auxAllowable[s] * auxBal) + ((1 - auxBal) * auxAllowable.Sum());
                auxMulti[s] = FacBal[s] * auxAllowable[s];
            }

            for (int s = 0; s < NumSpecies; s++)
            {
                wFrac = MathUtility.Divide(auxMulti[s], auxMulti.Sum(), 0.0);
                mySpecies[s].soilNuptake = totUptake * Math.Min(1.0, wFrac);

                for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                { // simple partition, ignoring root presence
                    wFrac = MathUtility.Divide(mySpecies[s].WaterDemand, swardWaterDemand, 0.0);
                    mySpecies[s].soilAvailableWater[layer] = PAW[layer] * Math.Min(1.0, wFrac);
                }
            }
        }
        else
        {
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {

                PAW[layer] = swardWaterUptakeByAPSIM[layer];
                for (int s = 0; s < NumSpecies; s++)
                {
                    wFrac = MathUtility.Divide(mySpecies[s].WaterDemand, swardWaterDemand, 1.0);
                    mySpecies[s].soilAvailableWater[layer] = PAW[layer] * Math.Min(1.0, wFrac);
                }
            }
        }

        return PAW;
    }

    /// <summary>
    /// Evaluate water supply vs. demand and compute water uptake
    /// </summary>
    private void WaterBudgetAndUptake()
    {
        // clear some variables
        Array.Clear(soilWaterUptake, 0, dlayer.Length);

        double totalWaterUptake = 0.0;
        if (WaterUptakeSource == "calc")
        {  // uptake is calculated by AgPasture
            if (usingAlternativeWUptake)
            { // consider root and species
                double[] auxDemand = new double[NumSpecies];
                double[] auxUptake = new double[NumSpecies];
                double[] auxFac = new double[NumSpecies];
                double wFac = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                    auxDemand[s] = Math.Min(mySpecies[s].WaterDemand, mySpecies[s].soilAvailableWater.Sum());

                double balFac = MathUtility.Divide(auxDemand.Sum() - Math.Min(soilAvailableWater.Sum(), auxDemand.Sum()), auxDemand.Sum(), 0.0);
                for (int s = 0; s < NumSpecies; s++)
                {
                    auxUptake[s] = (auxDemand[s] * balFac) + (1 - balFac) * auxDemand.Sum();
                    auxFac[s] = auxUptake[s] * auxDemand[s];
                }

                for (int s = 0; s < NumSpecies; s++)
                {
                    wFac = MathUtility.Divide(auxFac[s], auxFac.Sum(), 0.0);
                    mySpecies[s].soilWaterUptake = Math.Min(soilAvailableWater.Sum(), auxDemand.Sum()) * wFac;
                    totalWaterUptake += mySpecies[s].soilWaterUptake;
                }
            }
            else
            { // consider only whole sward
                totalWaterUptake = Math.Min(swardWaterDemand, soilAvailableWater.Sum());
                for (int s = 0; s < NumSpecies; s++)
                {
                    double wFrac = MathUtility.Divide(mySpecies[s].WaterDemand, swardWaterDemand, 0.0);
                    mySpecies[s].soilWaterUptake = totalWaterUptake * wFrac;
                }
            }

            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                soilWaterUptake[layer] = MathUtility.Divide(soilAvailableWater[layer] * totalWaterUptake, soilAvailableWater.Sum(), 0.0);
            }
        }
        else
        { // uptake was computed by external module (SWIM)
            if (usingAlternativeWUptake)
            { // check that each species can actually take up what swim calculated
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].soilWaterUptake = Math.Min(mySpecies[s].WaterDemand, mySpecies[s].soilAvailableWater.Sum());
                    totalWaterUptake += mySpecies[s].soilWaterUptake;
                }
            }
            else
            { // Uptake is equal to available
                totalWaterUptake = soilAvailableWater.Sum();
                for (int s = 0; s < NumSpecies; s++)
                    mySpecies[s].soilWaterUptake = mySpecies[s].soilAvailableWater.Sum();
            }

            // check for excess of water
            if (totalWaterUptake < swardWaterUptakeByAPSIM.Sum())
            {
                double[] XSwater = new double[dlayer.Length];
                for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                {
                    soilWaterUptake[layer] = soilAvailableWater[layer];
                    XSwater[layer] = soilWaterUptake[layer] - swardWaterUptakeByAPSIM[layer];
                    if (XSwater[layer] > 0.000001)
                        throw new Exception("Water uptake of AgPasture is greater than that given by SWIM");
                }

                if (XSwater.Sum() < 0.0)
                {
                    SendWaterChanges(XSwater);
                    Console.WriteLine("AgPasture is sending " + XSwater.Sum().ToString("#0.00#") + " mm of water back to soil module (uptake not used by plant)");
                }
            }
            else
            {
                for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                    soilWaterUptake[layer] = swardWaterUptakeByAPSIM[layer];
            }
        }
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract
    /// </summary>
    /// <returns>Amount of N available to plants</returns>
    private double PlantNExtractability()
    {
        // clear some variables
        Array.Clear(soilNH4Available, 0, dlayer.Length);
        Array.Clear(soilNO3Available, 0, dlayer.Length);
        for (int s = 0; s < NumSpecies; s++)
        {
            Array.Clear(mySpecies[s].soilAvailableNH4, 0, dlayer.Length);
            Array.Clear(mySpecies[s].soilAvailableNO3, 0, dlayer.Length);
        }

        if (NUptakeSource.ToLower() == "calc")
        {
            if (NExtractabilityMethod == 1)
            {
                return PlantExtractableSoilN_M1();
            }
            if (NExtractabilityMethod == 2)
            {
                return PlantExtractableSoilN_M2();
            }
            if (NExtractabilityMethod == 3)
            {
                return PlantExtractableSoilN_M3();
            }
            if (NExtractabilityMethod == 4)
            {
                return PlantExtractableSoilN_M4();
            }
            else // default method = 0
            {
                return PlantExtractableNClassic();
            }
        }
        else
        {
            return PlantExtractableSoilNApsim();

            // N uptake calculated by other modules (e.g., SWIM) - not implemented
            //throw new Exception("External N uptake is not implemented in AgPasture. Please specify N uptake source as \"calc\".");
        }
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract
    /// This is the 'classic' method, all N in the root zone is available
    /// </summary>
    /// <remarks>
    /// The method has been modified to consider limits to available N (as defined in SoilNitrogen)
    /// This is used when dealing with SoilCNPatches, limiting plants access to patches with very high N content
    /// </remarks>
    /// <returns>Amount of N available to plants</returns>
    private double PlantExtractableNClassic()
    {
        double totalAvailable = 0.0;
        double layerFrac = 1.0;      // fraction of each layer explored by roots
        double maxAvailable = 0.0;   // maximum N amount available in each layer
        double nFrac = 0.0;          // fraction available for each species
        if (usingNAvailableBySpecies)
        { // consider each species
            for (int s = 0; s < NumSpecies; s++)
            { //amount as if each species was alone
                for (int layer = 0; layer <= mySpecies[s].RootZoneBottomLayer(); layer++)
                {
                    layerFrac = LayerFractionForRoots(layer, mySpecies[s].rootDepth);
                    if (nh4_PlantAvailable == null)
                    {
                        mySpecies[s].soilAvailableNH4[layer] = nh4[layer] * layerFrac;
                        mySpecies[s].soilAvailableNO3[layer] = no3[layer] * layerFrac;
                    }
                    else
                    {
                        mySpecies[s].soilAvailableNH4[layer] = Math.Min(nh4[layer], nh4_PlantAvailable[layer]) * layerFrac;
                        mySpecies[s].soilAvailableNO3[layer] = Math.Min(no3[layer], no3_PlantAvailable[layer]) * layerFrac;
                    }
                    soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                }
            }

            // Adjust the values so that total available N is equal or less than maximum
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                // maximum N available for this layer
                layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                maxAvailable = (nh4[layer] + no3[layer]) * layerFrac;
                // adjust amounts for each species
                nFrac = MathUtility.Divide(maxAvailable, soilNH4Available[layer] + soilNO3Available[layer], 0.0);
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].soilAvailableNH4[layer] *= nFrac;
                    mySpecies[s].soilAvailableNO3[layer] *= nFrac;
                    soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }
        else
        { // consider whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                if (nh4_PlantAvailable == null)
                {
                    soilNH4Available[layer] = nh4[layer] * layerFrac;
                    soilNO3Available[layer] = no3[layer] * layerFrac;
                }
                else
                {
                    soilNH4Available[layer] = Math.Min(nh4[layer], nh4_PlantAvailable[layer]) * layerFrac;
                    soilNO3Available[layer] = Math.Min(no3[layer], no3_PlantAvailable[layer]) * layerFrac;
                }

                // partition amount to each species (simple approach, values not realy used)
                for (int s = 0; s < NumSpecies; s++)
                { // based on root DM
                    nFrac = MathUtility.Divide(mySpecies[s].dmroot, RootWt, 0.0);
                    mySpecies[s].soilAvailableNH4[layer] = soilNH4Available[layer] * nFrac;
                    mySpecies[s].soilAvailableNO3[layer] = soilNO3Available[layer] * nFrac;
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }

        return totalAvailable;
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract (method 1)
    /// This approach considers the amount of water in the soil as well as 
    /// an extractability factor for each N form
    /// </summary>
    /// <returns>Amount of N available to plants</returns>
    private double PlantExtractableSoilN_M1()
    {
        double totalAvailable = 0.0;
        double layerFrac = 1.0;      // fraction of each layer explored by roots
        double maxAvailable = 0.0;   // maximum N amount available in each layer
        double nFrac = 0.0;          // fraction available for each species
        double wSatFrac = 0.0;       // water saturation fraction

        if (usingNAvailableBySpecies)
        { // considering each species
            for (int s = 0; s < NumSpecies; s++)
            { //amount as if each species was alone
                for (int layer = 0; layer <= mySpecies[s].RootZoneBottomLayer(); layer++)
                {
                    wSatFrac = MathUtility.Divide(sw[layer] - ll[layer], DUL[layer] - ll[layer], 0.0);
                    wSatFrac = Math.Max(0.0, Math.Min(wSatFrac, 1.0));
                    wSatFrac = Math.Pow(wSatFrac, mySpecies[s].NextraSWF);
                    layerFrac = LayerFractionForRoots(layer, mySpecies[s].rootDepth);

                    mySpecies[s].soilAvailableNH4[layer] = nh4[layer] * layerFrac * kNH4[layer] * wSatFrac;
                    mySpecies[s].soilAvailableNO3[layer] = no3[layer] * layerFrac * kNO3[layer] * wSatFrac;
                    soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                }
            }

            // Adjust the values so that total available N is equal or less than maximum
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                // maximum N available for this layer
                layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                maxAvailable = (nh4[layer] + no3[layer]) * layerFrac;
                // adjust amounts for each species
                nFrac = MathUtility.Divide(maxAvailable, soilNH4Available[layer] + soilNO3Available[layer], 0.0);
                soilNH4Available[layer] = 0.0;
                soilNO3Available[layer] = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].soilAvailableNH4[layer] *= nFrac;
                    mySpecies[s].soilAvailableNO3[layer] *= nFrac;
                    soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }
        else
        { // consider whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                wSatFrac = MathUtility.Divide(sw[layer] - ll[layer], DUL[layer] - ll[layer], 0.0);
                wSatFrac = Math.Max(0.0, Math.Min(wSatFrac, 1.0));
                wSatFrac = Math.Pow(wSatFrac, NextraSWF[0]);
                layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                soilNH4Available[layer] = nh4[layer] * layerFrac * kNH4[layer] * wSatFrac;
                soilNO3Available[layer] = no3[layer] * layerFrac * kNO3[layer] * wSatFrac;

                // partition amount to each species (based on root DM only)
                for (int s = 0; s < NumSpecies; s++)
                { // based on root DM
                    nFrac = MathUtility.Divide(mySpecies[s].dmroot, RootWt, 0.0);
                    mySpecies[s].soilAvailableNH4[layer] = soilNH4Available[layer] * nFrac;
                    mySpecies[s].soilAvailableNO3[layer] = soilNO3Available[layer] * nFrac;
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }

        return totalAvailable;
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract (method 2)
    /// This approach considers root distribution as well as 
    /// an extractability factor for each N form
    /// </summary>
    /// <returns>Amount of N available to plants</returns>
    private double PlantExtractableSoilN_M2()
    {
        double totalAvailable = 0.0;
        double layerFrac = 1.0;         // fraction of each layer explored by roots
        double maxAvailable = 0.0;      // maximum N amount available in each layer
        double nFrac = 0.0;             // fraction available for each species
        double[] xFac = new double[2];  // extractability factor for each layer

        if (usingNAvailableBySpecies)
        { // considering each species
            for (int s = 0; s < NumSpecies; s++)
            { //amount as if each species was alone
                for (int layer = 0; layer <= mySpecies[s].RootZoneBottomLayer(); layer++)
                {
                    layerFrac = LayerFractionForRoots(layer, mySpecies[s].rootDepth);
                    if (mySpecies[s].referenceRLD > 0.0)
                    {
                        xFac[0] = Math.Min(1.0, kNH4[layer] * mySpecies[s].RLD[layer] / mySpecies[s].referenceRLD);
                        xFac[1] = Math.Min(1.0, kNO3[layer] * mySpecies[s].RLD[layer] / mySpecies[s].referenceRLD);
                    }
                    else
                    {
                        xFac[0] = Math.Min(1.0, kNH4[layer]);
                        xFac[1] = Math.Min(1.0, kNO3[layer]);
                    }

                    mySpecies[s].soilAvailableNH4[layer] = nh4[layer] * layerFrac * xFac[0];
                    mySpecies[s].soilAvailableNO3[layer] = no3[layer] * layerFrac * xFac[1];
                    soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                }
            }

            // Adjust the values so that total available N is equal or less than maximum
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                // maximum N available for this layer
                layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                maxAvailable = (nh4[layer] + no3[layer]) * layerFrac;
                // adjust amounts for each species
                nFrac = MathUtility.Divide(maxAvailable, soilNH4Available[layer] + soilNO3Available[layer], 0.0);
                soilNH4Available[layer] = 0.0;
                soilNO3Available[layer] = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].soilAvailableNH4[layer] *= nFrac;
                    mySpecies[s].soilAvailableNO3[layer] *= nFrac;
                    soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }
        else
        { // consider whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                if (referenceRLD[0] > 0.0)
                {
                    xFac[0] = Math.Min(1.0, kNH4[layer] * rlv[layer] / referenceRLD[0]);
                    xFac[1] = Math.Min(1.0, kNO3[layer] * rlv[layer] / referenceRLD[0]);
                }
                else
                {
                    xFac[0] = Math.Min(1.0, kNH4[layer]);
                    xFac[1] = Math.Min(1.0, kNO3[layer]);
                }
                soilNH4Available[layer] = nh4[layer] * layerFrac * xFac[0];
                soilNO3Available[layer] = no3[layer] * layerFrac * xFac[1];

                // partition amount to each species (based on root DM only)
                for (int s = 0; s < NumSpecies; s++)
                { // based on root DM
                    nFrac = MathUtility.Divide(mySpecies[s].dmroot, RootWt, 0.0);
                    mySpecies[s].soilAvailableNH4[layer] = soilNH4Available[layer] * nFrac;
                    mySpecies[s].soilAvailableNO3[layer] = soilNO3Available[layer] * nFrac;
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }

        return totalAvailable;
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract (method 3)
    /// This approach considers root distribution as well as 
    /// water content and an extractability factor for each N form
    /// </summary>
    /// <returns>Amount of N available to plants</returns>
    private double PlantExtractableSoilN_M3()
    {
        double totalAvailable = 0.0;
        double layerFrac = 1.0;         // fraction of each layer explored by roots
        double maxAvailable = 0.0;      // maximum N amount available in each layer
        double nFrac = 0.0;             // fraction available for each species
        double wSatFrac = 0.0;          // water saturation fraction
        double[] xFac = new double[2];  // extractability factor for each layer

        if (usingNAvailableBySpecies)
        { // considering each species
            for (int s = 0; s < NumSpecies; s++)
            { //amount as if each species was alone
                for (int layer = 0; layer <= mySpecies[s].RootZoneBottomLayer(); layer++)
                {
                    wSatFrac = MathUtility.Divide(sw[layer] - ll[layer], DUL[layer] - ll[layer], 0.0);
                    wSatFrac = Math.Max(0.0, Math.Min(wSatFrac, 1.0));
                    wSatFrac = Math.Pow(wSatFrac, mySpecies[s].NextraSWF);
                    layerFrac = LayerFractionForRoots(layer, mySpecies[s].rootDepth);
                    if (mySpecies[s].referenceRLD > 0.0)
                    {
                        xFac[0] = Math.Min(1.0, kNH4[layer] * wSatFrac * mySpecies[s].RLD[layer] / mySpecies[s].referenceRLD);
                        xFac[1] = Math.Min(1.0, kNO3[layer] * wSatFrac * mySpecies[s].RLD[layer] / mySpecies[s].referenceRLD);
                    }
                    else
                    {
                        xFac[0] = Math.Min(1.0, kNH4[layer] * wSatFrac);
                        xFac[1] = Math.Min(1.0, kNO3[layer] * wSatFrac);
                    }

                    mySpecies[s].soilAvailableNH4[layer] = nh4[layer] * layerFrac * xFac[0];
                    mySpecies[s].soilAvailableNO3[layer] = no3[layer] * layerFrac * xFac[1];
                    soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                }
            }

            // Adjust the values so that total available N is equal or less than maximum
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                // maximum N available for this layer
                layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                maxAvailable = (nh4[layer] + no3[layer]) * layerFrac;
                // adjust amounts for each species
                nFrac = MathUtility.Divide(maxAvailable, soilNH4Available[layer] + soilNO3Available[layer], 0.0);
                soilNH4Available[layer] = 0.0;
                soilNO3Available[layer] = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].soilAvailableNH4[layer] *= nFrac;
                    mySpecies[s].soilAvailableNO3[layer] *= nFrac;
                    soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }
        else
        { // consider whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                wSatFrac = MathUtility.Divide(sw[layer] - ll[layer], DUL[layer] - ll[layer], 0.0);
                wSatFrac = Math.Max(0.0, Math.Min(wSatFrac, 1.0));
                wSatFrac = Math.Pow(wSatFrac, NextraSWF[0]);
                layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                if (referenceRLD[0] > 0.0)
                {
                    xFac[0] = Math.Min(1.0, kNH4[layer] * wSatFrac * rlv[layer] / referenceRLD[0]);
                    xFac[1] = Math.Min(1.0, kNO3[layer] * wSatFrac * rlv[layer] / referenceRLD[0]);
                }
                else
                {
                    xFac[0] = Math.Min(1.0, kNH4[layer] * wSatFrac);
                    xFac[1] = Math.Min(1.0, kNO3[layer] * wSatFrac);
                }
                soilNH4Available[layer] = nh4[layer] * layerFrac * xFac[0];
                soilNO3Available[layer] = no3[layer] * layerFrac * xFac[1];

                // partition amount to each species (based on root DM only)
                for (int s = 0; s < NumSpecies; s++)
                { // based on root DM
                    nFrac = MathUtility.Divide(mySpecies[s].dmroot, RootWt, 0.0);
                    mySpecies[s].soilAvailableNH4[layer] = soilNH4Available[layer] * nFrac;
                    mySpecies[s].soilAvailableNO3[layer] = soilNO3Available[layer] * nFrac;
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }

        return totalAvailable;
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract (method 4)
    /// This approach considers a maximum uptake rate for each N form 
    /// </summary>
    /// <remarks>
    /// The method has been modified to use when dealing with SoilCNPatches (defined in SoilNitrogen)
    /// </remarks>
    /// <returns>Amount of N available to plants</returns>
    private double PlantExtractableSoilN_M4()
    {
        double totalAvailable = 0.0;
        double layerFrac = 1.0;         // fraction of each layer explored by roots
        double maxAvailable = 0.0;      // maximum N amount available in each layer
        double MaxUptakeNH4 = 0.0;      // maximum NH4 amount that can be taken up, in each layer
        double MaxUptakeNO3 = 0.0;      // maximum NO3 amount that can be taken up, in each layer
        double nFrac = 0.0;             // fraction available for each species

        if (usingNAvailableBySpecies)
        { // considering each species
            for (int s = 0; s < NumSpecies; s++)
            {
                if (PatchArea == null)
                { // there are no patches, so use 'classic' approach
                    //amount as if each species was alone
                    for (int layer = 0; layer <= mySpecies[s].RootZoneBottomLayer(); layer++)
                    {
                        layerFrac = LayerFractionForRoots(layer, mySpecies[s].rootDepth);
                        mySpecies[s].soilAvailableNH4[layer] = nh4[layer] * layerFrac;
                        mySpecies[s].soilAvailableNO3[layer] = no3[layer] * layerFrac;
                        soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                        soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                    }
                }
                else
                {  // go over each patch and get amount available as if each species was alone
                    int nPatches = PatchNH4.Patch.Length;
                    for (int layer = 0; layer <= mySpecies[s].RootZoneBottomLayer(); layer++)
                    {
                        MaxUptakeNH4 = MaximumUptakeRateNH4[0] * 0.01 * bd[layer] * dlayer[layer];
                        MaxUptakeNO3 = MaximumUptakeRateNO3[0] * 0.01 * bd[layer] * dlayer[layer];
                        layerFrac = LayerFractionForRoots(layer, mySpecies[s].rootDepth);
                        for (int k = 0; k < 1; k++)
                        {
                            mySpecies[s].soilAvailableNH4[layer] = Math.Min(PatchNH4.Patch[k].Value[layer], MaxUptakeNH4) * PatchArea[k] * layerFrac;
                            mySpecies[s].soilAvailableNO3[layer] = Math.Min(PatchNO3.Patch[k].Value[layer], MaxUptakeNO3) * PatchArea[k] * layerFrac;
                        }

                        soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                        soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                    }
                }
            }

            // Adjust the values so that total available N is equal or less than maximum
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                // maximum N available for this layer
                layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                if (PatchArea == null)
                { // there are no patches, so use 'classic' approach
                    maxAvailable = (nh4[layer] + no3[layer]) * layerFrac;
                }
                else
                {  // go over each patch
                    MaxUptakeNH4 = MaximumUptakeRateNH4[0] * 0.01 * bd[layer] * dlayer[layer];
                    MaxUptakeNO3 = MaximumUptakeRateNO3[0] * 0.01 * bd[layer] * dlayer[layer];
                    for (int k = 0; k < 1; k++)
                    {
                        maxAvailable = Math.Min(PatchNH4.Patch[k].Value[layer], MaxUptakeNH4) * PatchArea[k] * layerFrac
                                     + Math.Min(PatchNO3.Patch[k].Value[layer], MaxUptakeNH4) * PatchArea[k] * layerFrac;
                    }
                }
                // adjust amounts for each species
                nFrac = MathUtility.Divide(maxAvailable, soilNH4Available[layer] + soilNO3Available[layer], 0.0);
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].soilAvailableNH4[layer] *= nFrac;
                    mySpecies[s].soilAvailableNO3[layer] *= nFrac;
                    soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }
        else
        { // consider whole sward
            if (PatchArea == null)
            { // there are no patches, so use 'classic' approach
                for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                {
                    layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                    soilNH4Available[layer] += nh4[layer] * layerFrac;
                    soilNO3Available[layer] += no3[layer] * layerFrac;
                }
            }
            else
            { // go over each patch and get the available N
                int nPatches = PatchNH4.Patch.Length;
                for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                {
                    MaxUptakeNH4 = MaximumUptakeRateNH4[0] * 0.01 * bd[layer] * dlayer[layer];
                    MaxUptakeNO3 = MaximumUptakeRateNO3[0] * 0.01 * bd[layer] * dlayer[layer];
                    layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                    for (int k = 0; k < nPatches; k++)
                    {
                        double teste = nh4[layer] * 1;
                        soilNH4Available[layer] += Math.Min(PatchNH4.Patch[k].Value[layer], MaxUptakeNH4) * PatchArea[k] * layerFrac;
                        soilNO3Available[layer] += Math.Min(PatchNO3.Patch[k].Value[layer], MaxUptakeNO3) * PatchArea[k] * layerFrac;
                    }
                }
            }

            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                // partition amount to each species (simple approach, values not realy used)
                for (int s = 0; s < NumSpecies; s++)
                { // based on root DM
                    nFrac = MathUtility.Divide(mySpecies[s].dmroot, RootWt, 0.0);
                    mySpecies[s].soilAvailableNH4[layer] = soilNH4Available[layer] * nFrac;
                    mySpecies[s].soilAvailableNO3[layer] = soilNO3Available[layer] * nFrac;
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }

        return totalAvailable;
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract, or have extracted
    /// This approach does not actually computes extractable N, only collects the values
    /// it has been computed somewhere else (SoilNitrogen) for each N form
    /// </summary>
    /// <returns>Amount of N available to plants</returns>
    private double PlantExtractableSoilNApsim()
    {
        // check that we have an input from apsim
        if (swardWaterUptakeByAPSIM == null)
            throw new Exception("No module provided an estimate for N uptake, check SoilN module or set NUptakeSource to \"calc\"");

        if (usingNAvailableBySpecies)
        { // consider each species  -  Not implemented
            throw new Exception("Procedure not implemented");
        }
        else
        { // consider whole sward
            double totalAvailable = 0.0;
            double nFrac = 0.0;
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                soilNH4Available[layer] = swardNH4UptakeByAPSIM[layer];
                soilNO3Available[layer] = swardNO3UptakeByAPSIM[layer];

                // partition amount to each species (simple approach, values not realy used)
                for (int s = 0; s < NumSpecies; s++)
                { // based on root DM
                    nFrac = MathUtility.Divide(mySpecies[s].dmroot, RootWt, 0.0);
                    mySpecies[s].soilAvailableNH4[layer] = soilNH4Available[layer] * nFrac;
                    mySpecies[s].soilAvailableNO3[layer] = soilNO3Available[layer] * nFrac;
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
            return totalAvailable;
        }
    }

    /// <summary>
    /// Evaluate N budget and uptake processes
    /// </summary>
    /// <remarks>
    /// RCichota, Jun 2014: cleaned up and add consideration for remobilisation of luxury N
    /// RCichota, Dec 2014: separated calculation for glfN and actual uptake
    /// </remarks>
    private void NBudgetAndUptake()
    {
        //1) Get the total N demand (species by species)
        swardNFixed = 0.0;
        double p_Ndemand = 0.0;
        double p_NdemandOpt = 0.0;
        for (int s = 0; s < NumSpecies; s++)
        {
            swardNFixed += mySpecies[s].CalcNdemand();       //minimum N fixation
            p_NdemandOpt += mySpecies[s].NdemandOpt;    //demand for optimum [N]
            p_Ndemand += mySpecies[s].NdemandLux;       //demand for luxury [N]
        }

        //2) Update Nfix of legume species under N stress
        double Nstress = 1.0;
        if (p_Ndemand > 0.0 && (p_Ndemand > swardSoilNavailable + swardNFixed))
            Nstress = swardSoilNavailable / (p_Ndemand - swardNFixed);

        for (int s = 0; s < NumSpecies; s++)
        {
            if (mySpecies[s].isLegume && (Nstress < 0.99))  //more fixation under Nstress
            {
                double newNfix = mySpecies[s].MaxFix;
                newNfix -= (mySpecies[s].MaxFix - mySpecies[s].MinFix) * Nstress;
                newNfix *= mySpecies[s].NdemandOpt;
                double moreNfix = Math.Max(0.0, newNfix - mySpecies[s].Nfix);
                mySpecies[s].Nfix = newNfix;
                swardNFixed += moreNfix;
            }
        }

        //3) Get N remobilised and calculate N demand from soil
        swardSoilNDemand = 0.0;
        for (int s = 0; s < NumSpecies; s++)
        {
            if (mySpecies[s].NdemandLux <= mySpecies[s].Nremob + mySpecies[s].Nfix)
            {
                // Nremob and Nfix are able to supply all N - note: Nfix = 0 for non-legumes
                mySpecies[s].remob2NewGrowth = Math.Max(0.0, mySpecies[s].NdemandLux - mySpecies[s].Nfix);
                mySpecies[s].Nremob -= mySpecies[s].remob2NewGrowth;
                mySpecies[s].soilNdemand = 0.0;
            }
            else
            {
                // not enough N within the plant, uptake is needed
                mySpecies[s].remob2NewGrowth = mySpecies[s].Nremob;
                mySpecies[s].Nremob = 0.0;
                mySpecies[s].soilNdemand = mySpecies[s].NdemandLux - (mySpecies[s].Nfix + mySpecies[s].remob2NewGrowth);
            }

            mySpecies[s].newGrowthN = mySpecies[s].remob2NewGrowth + mySpecies[s].Nfix;
            swardSoilNDemand += mySpecies[s].soilNdemand;
        }

        //4) Compute soil N uptake, newGrowthN and N limitation factor
        for (int s = 0; s < NumSpecies; s++)
        {
            if (mySpecies[s].soilNdemand == 0.0)
            {
                mySpecies[s].soilNuptake = 0.0;
                mySpecies[s].NFastRemob3 = 0.0;
                mySpecies[s].NFastRemob2 = 0.0;
            }
            else
            {
                if (swardSoilNavailable >= swardSoilNDemand)
                {
                    // soil can supply all remaining N needed
                    mySpecies[s].soilNuptake = mySpecies[s].soilNdemand;
                    mySpecies[s].NFastRemob3 = 0.0;
                    mySpecies[s].NFastRemob2 = 0.0;
                    mySpecies[s].newGrowthN += mySpecies[s].soilNuptake;
                }
                else
                {
                    // soil cannot supply all N needed. Get the available N and partition between species
                    mySpecies[s].soilNuptake = swardSoilNavailable * MathUtility.Divide(mySpecies[s].soilNdemand, swardSoilNDemand, 0.0);
                    mySpecies[s].newGrowthN += mySpecies[s].soilNuptake;

                    // check whether demand for optimum growth is satisfied
                    if (mySpecies[s].NdemandOpt > mySpecies[s].newGrowthN)
                    {
                        // plant still needs more N for optimum growth (luxury uptake is ignored), check whether luxury N in plants can be used
                        double Nmissing = mySpecies[s].NdemandOpt - mySpecies[s].newGrowthN;
                        if (Nmissing <= mySpecies[s].NLuxury2 + mySpecies[s].NLuxury3)
                        {
                            // There is luxury N that can be used for optimum growth, first from tissue 3
                            if (Nmissing <= mySpecies[s].NLuxury3)
                            {
                                mySpecies[s].NFastRemob3 = Nmissing;
                                mySpecies[s].NFastRemob2 = 0.0;
                                Nmissing = 0.0;
                            }
                            else
                            {
                                mySpecies[s].NFastRemob3 = mySpecies[s].NLuxury3;
                                Nmissing -= mySpecies[s].NLuxury3;

                                // remaining from tissue 2
                                mySpecies[s].NFastRemob2 = Nmissing;
                                Nmissing = 0.0;
                            }
                        }
                        else
                        {
                            // N luxury is not enough for optimum growth, use up all there is
                            if (mySpecies[s].NLuxury2 + mySpecies[s].NLuxury3 > 0.0)
                            {
                                mySpecies[s].NFastRemob3 = mySpecies[s].NLuxury3;
                                Nmissing -= mySpecies[s].NLuxury3;
                                mySpecies[s].NFastRemob2 = mySpecies[s].NLuxury2;
                                Nmissing -= mySpecies[s].NLuxury2;
                            }
                        }

                        mySpecies[s].newGrowthN += mySpecies[s].NFastRemob3 + mySpecies[s].NFastRemob2;
                    }
                    else
                    {
                        // N supply is enough for optimum growth, although luxury uptake is not fully accomplished
                        mySpecies[s].NFastRemob3 = 0.0;
                        mySpecies[s].NFastRemob2 = 0.0;
                    }
                }
            }
        }

        //5) Compute partition of N uptake for each N form and layer
        PartitionNUptake();
    }

    /// <summary>
    /// Evaluate the amount of N taken up from each layer, for each N form
    /// </summary>
    private void PartitionNUptake()
    {
        // clear some variables
        Array.Clear(soilNH4Uptake, 0, dlayer.Length);
        Array.Clear(soilNO3Uptake, 0, dlayer.Length);

        if (usingAlternativeNUptake)
        { // consider partition of uptake for each species
            double fraction = 0.0;
            double totNAvail = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                if (mySpecies[s].soilNuptake > 0.0)
                { // there is some uptake

                    // partition between N forms
                    totNAvail = mySpecies[s].soilAvailableNH4.Sum() + mySpecies[s].soilAvailableNO3.Sum();
                    fraction = MathUtility.Divide(mySpecies[s].soilAvailableNH4.Sum(), totNAvail, 0.0);
                    mySpecies[s].soilNH4Uptake = mySpecies[s].soilNuptake * fraction;
                    mySpecies[s].soilNO3Uptake = mySpecies[s].soilNuptake * (1 - fraction);

                    // partition amongst layers
                    fraction = MathUtility.Divide(mySpecies[s].soilNuptake, totNAvail, 0.0);
                    for (int layer = 0; layer <= mySpecies[s].RootZoneBottomLayer(); layer++)
                    {
                        soilNH4Uptake[layer] += mySpecies[s].soilAvailableNH4[layer] * fraction;
                        soilNO3Uptake[layer] += mySpecies[s].soilAvailableNO3[layer] * fraction;
                    }
                }
                else
                { // No uptake, zero variables
                    mySpecies[s].soilNH4Uptake = 0.0;
                    mySpecies[s].soilNO3Uptake = 0.0;
                }
            }
        }
        else
        { // uptake partition ignores species
            double totalNuptake = mySpecies.Sum(x => x.soilNuptake);

            if (totalNuptake > 0.0)
            {
                // partition uptake amongst layers
                double fraction = Math.Min(1.0, MathUtility.Divide(totalNuptake, swardSoilNavailable, 0.0));
                for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                {
                    soilNH4Uptake[layer] = soilNH4Available[layer] * fraction;
                    soilNO3Uptake[layer] = soilNO3Available[layer] * fraction;
                }

                // partition uptake between N form for each species
                fraction = MathUtility.Divide(soilNH4Uptake.Sum(), totalNuptake, 0.0);
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].soilNH4Uptake = mySpecies[s].soilNuptake * fraction;
                    mySpecies[s].soilNO3Uptake = mySpecies[s].soilNuptake * (1 - fraction);
                }
            }
            else
            { // no uptake, zero variables
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].soilNH4Uptake = 0.0;
                    mySpecies[s].soilNO3Uptake = 0.0;
                }
            }
        }

        // check that amounts to remove aren't larger that available
        for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
        {
            if ((soilNH4Uptake[layer] - nh4[layer]) > 0.000001)
                throw new Exception("Loss of mass balance - NH4 uptake too large");

            if ((soilNO3Uptake[layer] - no3[layer]) > 0.000001)
                throw new Exception("Loss of mass balance - NO3 uptake too large");
        }
    }

    /// <summary>
    /// Perform the actual changes in soil water and N content due to plant uptake
    /// </summary>
    private void UptakeWaterAndN()
    {
        // do the actual water uptake
        if (soilWaterUptake.Sum() > 0.0)
            SendWaterChanges(soilWaterUptake);

        // do actual N uptake
        if (soilNH4Uptake.Sum() + soilNO3Uptake.Sum() > 0.0)
            if ((NExtractabilityMethod == 4) && (PatchArea != null))
            { // uptake should be 'patch-aware' (to use when dealing with SoilCNPatches)
                int nPatches = PatchNH4.Patch.Length;
                double layerFrac = 0.0;
                double patchFrac = 0.0;
                double MaxNUptakeLayer = 0.0;
                double AvailableNLayer = 0.0;
                double[] patchUptakeNH4 = new double[dlayer.Length];
                double[] patchUptakeNO3 = new double[dlayer.Length];
                double TotalUptake = 0.0;
                for (int k = 0; k < nPatches; k++)
                { // partition for each patch
                    for (int layer = 0; layer < dlayer.Length; layer++)
                    {
                        layerFrac = LayerFractionForRoots(layer, swardRootDepth);
                        patchUptakeNH4[layer] = 0.0;
                        patchUptakeNO3[layer] = 0.0;
                        if (soilNH4Available[layer] > 0.0)
                        {
                            MaxNUptakeLayer = MaximumUptakeRateNH4[0] * 0.01 * bd[layer] * dlayer[layer];
                            AvailableNLayer = Math.Min(PatchNH4.Patch[k].Value[layer], MaxNUptakeLayer) * PatchArea[k] * layerFrac;
                            patchFrac = MathUtility.Divide(AvailableNLayer, soilNH4Available[layer], 0.0);
                            patchUptakeNH4[layer] = -(soilNH4Uptake[layer] * patchFrac) / PatchArea[k];
                        }
                        if (soilNO3Available[layer] > 0.0)
                        {
                            MaxNUptakeLayer = MaximumUptakeRateNO3[0] * 0.01 * bd[layer] * dlayer[layer];
                            AvailableNLayer = Math.Min(PatchNO3.Patch[k].Value[layer], MaxNUptakeLayer) * PatchArea[k] * layerFrac;
                            patchFrac = MathUtility.Divide(AvailableNLayer, soilNO3Available[layer], 0.0);
                            patchUptakeNO3[layer] = -(soilNO3Uptake[layer] * patchFrac) / PatchArea[k];
                        }
                    }
                    // send the N changes
                    SendNitrogenChangesByPatch(k, patchUptakeNH4, patchUptakeNO3);
                    TotalUptake += (patchUptakeNH4.Sum() + patchUptakeNO3.Sum()) * PatchArea[k];
                }

                // check that mass balance was kept
                if (Math.Abs(soilNH4Uptake.Sum() + soilNO3Uptake.Sum() + TotalUptake) > 0.000001)
                    throw new Exception("Loss of mass balance - partition between SoilCNPatches");
            }
            else
            {
                SendNitrogenChanges(soilNH4Uptake, soilNO3Uptake);
            }
    }

    /// <summary>
    /// Send info about water changes to soil module
    /// </summary>
    /// <param name="WAmount">Delta water amount for each soil layer</param>
    private void SendWaterChanges(double[] WAmount)
    {
        // initialise water uptake data type
        WaterChangedType WaterUptake = new WaterChangedType();
        WaterUptake.DeltaWater = new double[dlayer.Length];

        // set the amounts to send
        for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            WaterUptake.DeltaWater[layer] = -WAmount[layer];

        if (WaterChanged != null)
            WaterChanged.Invoke(WaterUptake);
    }

    /// <summary>
    /// Send info about N changes to soil module
    /// </summary>
    /// <param name="NH4Amount">Delta NH4 amount for each soil layer</param>
    /// <param name="NO3Amount">Delta NO3 amount for each soil layer</param>
    private void SendNitrogenChanges(double[] NH4Amount, double[] NO3Amount)
    {
        // initialise water uptake data type
        NitrogenChangedType NUptake = new NitrogenChangedType();
        NUptake.Sender = thisCropName;
        NUptake.SenderType = "Plant";
        NUptake.DeltaNO3 = new double[dlayer.Length];
        NUptake.DeltaNH4 = new double[dlayer.Length];

        // set the amounts to send
        for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
        {
            NUptake.DeltaNH4[layer] = -NH4Amount[layer];
            NUptake.DeltaNO3[layer] = -NO3Amount[layer];
        }

        if (NitrogenChanged != null)
            NitrogenChanged.Invoke(NUptake);
    }

    /// <summary>
    /// Send info about N changes to soil module (consider SoilCNPatches)
    /// </summary>
    /// <param name="PatchToAddTo">Patch that will get this change</param>
    /// <param name="NH4Amount">Amount of NH4 change, for each layer</param>
    /// <param name="NO3Amount">Amount of NO3 change, for each layer</param>
    private void SendNitrogenChangesByPatch(int PatchToAddTo, double[] NH4Amount, double[] NO3Amount)
    {
        AddSoilCNPatchType PatchData = new AddSoilCNPatchType();
        PatchData.Sender = "AgPasture";
        PatchData.DepositionType = "ToSpecificPatch";
        PatchData.AreaFraction = 1.0;
        PatchData.AffectedPatches_id = new int[] { PatchToAddTo };
        PatchData.NH4 = NH4Amount;
        PatchData.NO3 = NO3Amount;

        if (AddSoilCNPatch != null)
            AddSoilCNPatch.Invoke(PatchData);
    }

    /// <summary>
    /// Val's method for N uptake (not fully implemented)
    /// </summary>
    private void ValsMethod()
    {
        double
            uptake_multiplier = double.MaxValue,
            totSWUptake = soilWaterUptake.Sum();

        double[]
        availableNH4_bylayer = new double[dlayer.Length],
        availableNO3_bylayer = new double[dlayer.Length],
        diffNH4_bylayer = new double[dlayer.Length],
        diffNO3_bylayer = new double[dlayer.Length];

        for (int sLayer = 0; sLayer < dlayer.Length; sLayer++)
        {
            double
            totN = nh4[sLayer] + no3[sLayer],
            fracH2O = soilWaterUptake[sLayer] / totSWUptake;

            if (totN > 0.0)
            {
                availableNH4_bylayer[sLayer] = fracH2O * nh4[sLayer] / totN;
                availableNO3_bylayer[sLayer] = fracH2O * no3[sLayer] / totN;

                //if we have no3 and nh4 in this layer then calculate our uptake multiplier, otherwise set it to 0
                //the idea behind the multiplier is that it allows us to calculate the max amount of N we can extract
                //without forcing any of the layers below 0 AND STILL MAINTAINING THE RATIO as calculated with fracH2O
                //NOTE: it doesn't matter whether we use nh4 or no3 for this calculation, we will get the same answer regardless
                uptake_multiplier = nh4[sLayer] * no3[sLayer] > 0.0 ? Math.Min(uptake_multiplier, nh4[sLayer] / availableNH4_bylayer[sLayer]) : 0;
            }
            else
            {
                availableNH4_bylayer[sLayer] = 0.0;
                availableNO3_bylayer[sLayer] = 0.0;
            }
        }

        //adjust availability values with the multiplier we just calculated
        availableNH4_bylayer = availableNH4_bylayer.Select(x => x * uptake_multiplier).ToArray();
        availableNO3_bylayer = availableNO3_bylayer.Select(x => x * uptake_multiplier).ToArray();

        //calculate how much no3/nh4 will be left in the soil layers (diff_nxx[layer] = nxx[layer] - availableNH4_bylayer[layer])
        diffNH4_bylayer = nh4.Select((x, sLayer) => Math.Max(0, x - availableNH4_bylayer[sLayer])).ToArray();
        diffNO3_bylayer = no3.Select((x, sLayer) => Math.Max(0, x - availableNO3_bylayer[sLayer])).ToArray();

        //adjust this by the sum of all leftover so we get a ratio we can use later
        double sum_diff = diffNH4_bylayer.Sum() + diffNO3_bylayer.Sum();
        diffNH4_bylayer = diffNH4_bylayer.Select(x => x / sum_diff).ToArray();
        diffNO3_bylayer = diffNO3_bylayer.Select(x => x / sum_diff).ToArray();

        double avail_withwater = availableNH4_bylayer.Sum() + availableNO3_bylayer.Sum();
        double SoilNuptake = soilNH4Uptake.Sum() + soilNO3Uptake.Sum();
        double shortfall_withwater = SoilNuptake - avail_withwater;

        if (shortfall_withwater > 0.0)
        {
            //this cap should not be needed because shortfall is already capped via the math.min in the scaled_demand calcs (leave it here though)
            double scaled_diff = Math.Min(shortfall_withwater / avail_withwater, 1);

            availableNH4_bylayer = availableNH4_bylayer.Select((x, sLayer) => x + (shortfall_withwater * diffNH4_bylayer[sLayer])).ToArray();
            availableNO3_bylayer = availableNO3_bylayer.Select((x, sLayer) => x + (shortfall_withwater * diffNO3_bylayer[sLayer])).ToArray();
        }

        soilNH4Uptake = availableNH4_bylayer.Select(x => x * 1).ToArray();
        soilNO3Uptake = availableNO3_bylayer.Select(x => x * 1).ToArray();

        double[] diffs = soilNO3Uptake.Select((x, i) => Math.Max(no3[i] + x + 0.00000001, 0)).ToArray();
        if (diffs.Any(x => x == 0))
            throw new Exception();
    }

    /// <summary>
    /// Set soil moisture stress factor to each species
    /// </summary>
    /// <remarks>Worth more efforts in this area (F. Li)</remarks>
    private void SetSpeciesGLFWater()
    {
        if (swardWaterDemand == 0)
        {
            swardGLFWater = 1.0;
            for (int s = 0; s < NumSpecies; s++)
                mySpecies[s].glfWater = swardGLFWater;
        }
        else if (soilWaterUptake.Sum() == 0)
        {
            swardGLFWater = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                mySpecies[s].glfWater = swardGLFWater;
        }
        else
        {
            if (usingWAvailableBySpecies)
            {
                double accum_gfwater = 0.0;
                double accum_LAI = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].glfWater = mySpecies[s].soilWaterUptake / mySpecies[s].WaterDemand;
                    if (mySpecies[s].glfWater > 0.999)
                    {
                        double mySW = 0.0;       //soil water content
                        double mySat = 0.0;      //water content at saturation
                        double myDUL = 0.0;      //water contenct at field capacity
                        for (int layer = 0; layer <= mySpecies[s].RootZoneBottomLayer(); layer++)
                        {
                            mySW += sw_dep[layer];
                            mySat += SAT_dep[layer];
                            myDUL += DUL[layer] * dlayer[layer];
                        }
                        if (mySW > myDUL)
                        {
                            mySpecies[s].glfWater = 1 - (mySpecies[s].soilSatFactor * (mySW - myDUL) / (mySat - myDUL));
                        }
                    }

                    accum_gfwater += mySpecies[s].glfWater * mySpecies[s].greenLAI;
                    accum_LAI += mySpecies[s].greenLAI;
                }

                if (swardGreenLAI > 0.0)
                    swardGLFWater = accum_gfwater / swardGreenLAI;
                else
                    swardGLFWater = 1.0;
            }
            else
            {
                swardGLFWater = soilWaterUptake.Sum() / swardWaterDemand;
                if (swardGLFWater > 0.999)  //possible saturation
                {
                    // calculate soil moisture content in root zone
                    double mySW = 0.0;       //soil water content
                    double mySat = 0.0;      //water content at saturation
                    double myDUL = 0.0;      //water contenct at field capacity
                    for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                    {
                        mySW += sw_dep[layer];
                        mySat += SAT_dep[layer];
                        myDUL += DUL[layer] * dlayer[layer];
                    }

                    if (mySW > myDUL) //if saturated
                    {
                        double accum_gfwater = 0.0;
                        double accum_LAI = 0.0;
                        for (int s = 0; s < NumSpecies; s++)
                        {
                            mySpecies[s].glfWater = 1 - (mySpecies[s].soilSatFactor * (mySW - myDUL) / (mySat - myDUL));
                            accum_gfwater += mySpecies[s].glfWater * mySpecies[s].greenLAI;
                            accum_LAI += mySpecies[s].greenLAI;
                        }

                        if (accum_LAI > 0.0)
                            swardGLFWater = accum_gfwater / accum_LAI;
                        else
                            swardGLFWater = 1.0;
                        return;
                    }
                }

                // if glf was not updated for each species do it now
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].glfWater = swardGLFWater;
                }
            }
        }
    }

    /// <summary>
    /// Set soil N stress factor to each species
    /// </summary>
    private void SetSpeciesGLFNitrogen()
    {
        //weighted average of species glfN
        if (swardPotGrowthWater > 0.0)
        {
            swardGLFN = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                if (mySpecies[s].NdemandOpt > 0.0)
                {
                    mySpecies[s].glfN = Math.Min(1.0, Math.Max(0.0, mySpecies[s].newGrowthN / mySpecies[s].NdemandOpt));
                }
                else
                {
                    mySpecies[s].glfN = 1.0;
                }
                swardGLFN += mySpecies[s].glfN * mySpecies[s].dGrowthW / swardPotGrowthWater;
            }
        }
        else
        {
            for (int s = 0; s < NumSpecies; s++)
            {
                mySpecies[s].glfN = 1.0;
            }
            swardGLFN = 1.0;
        }
    }

    /// <summary>
    /// Partitioning plant growth and tissue turnover
    /// </summary>
    private void GrowthAndPartition()
    {
        // reset some variables
        swardGreenLAI = 0.0;
        swardDeadLAI = 0.0;
        swardGreenDM = 0.0;
        swardDeadDM = 0.0;
        swardHerbageGrowth = 0.0;
        swardRootDM = 0.0;
        swardLitterDM = 0.0;
        swardLitterN = 0.0;
        swardSenescedRootDM = 0.0;
        swardSenescedRootN = 0.0;

        for (int s = 0; s < NumSpecies; s++)
        {
            // Compute the partitioning of DM grown
            mySpecies[s].PartitionDMGrown();

            // Compute the tissue turnover
            mySpecies[s].TissueTurnover();

            // update aggregated variables
            mySpecies[s].UpdateAggregated();

            swardGreenDM += mySpecies[s].dmgreen;
            swardDeadDM += mySpecies[s].dmdead;
            swardRootDM += mySpecies[s].dmroot;
            swardHerbageGrowth += mySpecies[s].dmshoot - mySpecies[s].prevState.dmshoot;
            swardLitterDM += mySpecies[s].dLitter;
            swardLitterN += mySpecies[s].dNLitter;
            swardSenescedRootDM += mySpecies[s].dRootSen;
            swardSenescedRootN += mySpecies[s].dNrootSen;

            // update plant parts (LAI, height, root)
            mySpecies[s].UpdatePlantParts();

            swardGreenLAI += mySpecies[s].greenLAI;
            swardDeadLAI += mySpecies[s].deadLAI;

            // Calc todays digestibility
            mySpecies[s].calcDigestibility();
        }

        swardTotalLAI = swardGreenLAI + swardDeadLAI;
        swardShootDM = swardGreenDM + swardDeadDM;

        // Return litter to surface OM
        DoSurfaceOMReturn(swardLitterDM, swardLitterN, 1.0);

        // Return senesced root to soil FOM
        DoIncorpFomEvent(swardSenescedRootDM, swardSenescedRootN);

        // RCichota May2014: zero out the stored pS.dmdefoliated (it has been used today)
        for (int s = 0; s < NumSpecies; s++)
        {
            mySpecies[s].prevState.dmdefoliated = 0.0;
        }
    }

    #endregion  -----------------------------------------------------------------------------------

    #region Additional event handlers  ------------------------------------------------------------

    /// <summary>
    /// Respond to Sow event
    /// </summary>
    /// <param name="PSow">Sow</param>
    [EventHandler]
    public void OnSow(SowType PSow)
    {
        /*SowType is our type and is defined like this:
        <type name="Sow">
        <field name="Cultivar" kind="string" />
        <field name="Population" kind="double" />
        <field name="Depth" kind="double" />
        <field name="MaxCover" kind="double" />
        <field name="BudNumber" kind="double" />
        </type>
        */

        isAlive = true;
        ZeroVars();
        for (int s = 0; s < NumSpecies; s++)
            mySpecies[s].SetInGermination();
    }

    /// <summary>
    /// Respond to a Kill event
    /// </summary>
    /// <param name="PKill">Kill</param>
    [EventHandler]
    public void OnKillCrop(KillCropType PKill)
    {
        double frac = PKill.KillFraction;
        //always complete kill for pasture, ignore fraction

        //Above_ground part returns to surface OM comletey (frac = 1.0)
        DoSurfaceOMReturn(swardShootDM, AboveGroundN, 1.0);    //n_shoot

        //Incorporate root mass in soil fresh organic matter
        DoIncorpFomEvent(swardRootDM, BelowGroundN);         //n_root);

        ZeroVars();

        isAlive = false;
    }

    /// <summary>
    /// Zero out some variables
    /// </summary>
    private void ZeroVars()
    {
        //shoot
        swardGreenLAI = 0.0;
        swardDeadLAI = 0.0;
        swardTotalLAI = 0.0;
        swardGreenDM = 0.0;
        swardDeadDM = 0.0;
        swardShootDM = 0.0;
        swardHeight = 0.0;

        //root
        swardRootDM = 0.0;
        swardRootDepth = 0.0;

        //daily changes
        swardPotentialGrowth = swardPotGrowthWater = swardActualGrowth = swardHerbageGrowth = 0.0;   //daily DM increase
        swardLitterDM = swardLitterN = 0.0;
        swardSenescedRootDM = swardSenescedRootN = 0.0;

        swardWaterDemand = 0.0;
        swardSoilNDemand = 0.0;

        //species (ignore fraction)
        for (int s = 0; s < NumSpecies; s++)
            mySpecies[s].ResetZero();
    }

    /// <summary>
    /// Return plant litter to surface organic matter poor
    /// </summary>
    /// <param name="amtDM">DM amount</param>
    /// <param name="amtN">N amount</param>
    /// <param name="frac">Fraction=1</param>
    private void DoSurfaceOMReturn(double amtDM, double amtN, double frac)
    {
        float dDM = (float)amtDM;

        BiomassRemovedType BR = new BiomassRemovedType();
        string[] type = new string[1];
        float[] dltdm = new float[1];
        float[] dltn = new float[1];
        float[] dltp = new float[1];
        float[] fraction = new float[1];

        type[0] = "grass";
        dltdm[0] = dDM;                 // kg/ha
        dltn[0] = (float)amtN;         // dDM * (float)dead_nconc;
        dltp[0] = dltn[0] * 0.3F;       //just a stub here, no P budgeting process in this module
        fraction[0] = (float)frac;

        BR.crop_type = "grass";
        BR.dm_type = type;
        BR.dlt_crop_dm = dltdm;
        BR.dlt_dm_n = dltn;
        BR.dlt_dm_p = dltp;
        BR.fraction_to_residue = fraction;
        BiomassRemoved.Invoke(BR);
    }

    /// <summary>
    /// Return scenesced roots into fresh organic matter pool in soil
    /// </summary>
    /// <param name="rootSen">DM amount</param>
    /// <param name="NinRootSen">N amount</param>
    private void DoIncorpFomEvent(double rootSen, double NinRootSen)
    {
        FOMLayerLayerType[] fomLL = new FOMLayerLayerType[dlayer.Length];

        // ****  RCichota, Jun, 2014 change how RootFraction (rlvp) is used in here ****************************************
        // root senesced are returned to soil (as FOM) considering return is proportional to root mass

        double dAmtLayer = 0.0; //amount of root litter in a layer
        double dNLayer = 0.0;
        for (int i = 0; i < dlayer.Length; i++)
        {
            dAmtLayer = rootSen * RootFraction[i];
            dNLayer = NinRootSen * RootFraction[i];

            float amt = (float)dAmtLayer;

            FOMType fom = new FOMType();
            fom.amount = amt;
            fom.N = (float)dNLayer; // 0.03F * amt;    // N in dead root
            fom.C = 0.40F * amt;    //40% of OM is C. Actually, 'C' is not used, as shown in DataTypes.xml
            fom.P = 0.0F;              //to consider later
            fom.AshAlk = 0.0F;         //to consider later

            FOMLayerLayerType Layer = new FOMLayerLayerType();
            Layer.FOM = fom;
            Layer.CNR = 0.0F;       //not used
            Layer.LabileP = 0.0F;   //not used

            fomLL[i] = Layer;
        }

        FOMLayerType FomLayer = new FOMLayerType();
        FomLayer.Type = thisCropName;
        FomLayer.Layer = fomLL;
        IncorpFOM.Invoke(FomLayer);
    }

    /// <summary>
    /// Respond to a RemoveCropBiomass event
    /// </summary>
    /// <param name="rm">RemoveCropBiomass</param>
    [EventHandler]
    public void Onremove_crop_biomass(RemoveCropBiomassType rm)
    {
        //Note: It is resposibility of the calling module to check the
        // amount of herbage in each pools of AbovegroundBiomassWt and set the
        // the correct amount in 'rm'.
        // No checking if the removing amount passed in are too much here

        const double gm2ha = 10;   // constant for conversion of g/m^2 to kg/ha,
        // rm.dm.dlt should be in g/m^2

        double dm_leaf_green = LeafLiveWt;
        double dm_stem_green = StemLiveWt;
        double dm_leaf_dead = LeafDeadWt;
        double dm_stem_dead = StemDeadWt;

        for (int s = 0; s < NumSpecies; s++)     // for accumulating the total DM & N removal of species from verious pools
        {
            mySpecies[s].dmdefoliated = 0.0;
            mySpecies[s].Ndefoliated = 0.0;
        }

        for (int i = 0; i < rm.dm.Length; i++)              //for each pool
        {
            for (int j = 0; j < rm.dm[i].dlt.Length; j++)   //for each part
            {
                if (rm.dm[i].pool == "green" && rm.dm[i].part[j] == "leaf")
                {
                    for (int s = 0; s < NumSpecies; s++)           //for each species
                    {
                        if (dm_leaf_green != 0)             //resposibility of other modules to check the amount
                        {
                            double rm_leaf = gm2ha * rm.dm[i].dlt[j] * mySpecies[s].dmleaf_green / dm_leaf_green;
                            double rm_leaf1 = rm_leaf * MathUtility.Divide(mySpecies[s].dmleaf1, mySpecies[s].dmleaf_green, 0.0);
                            double rm_leaf2 = rm_leaf * MathUtility.Divide(mySpecies[s].dmleaf2, mySpecies[s].dmleaf_green, 0.0);
                            double rm_leaf3 = rm_leaf * MathUtility.Divide(mySpecies[s].dmleaf3, mySpecies[s].dmleaf_green, 0.0);
                            mySpecies[s].dmleaf1 -= rm_leaf1;
                            mySpecies[s].dmleaf2 -= rm_leaf2;
                            mySpecies[s].dmleaf3 -= rm_leaf3;
                            mySpecies[s].dmdefoliated += rm_leaf1 + rm_leaf2 + rm_leaf3;

                            mySpecies[s].Nleaf1 -= mySpecies[s].Ncleaf1 * rm_leaf1;
                            mySpecies[s].Nleaf2 -= mySpecies[s].Ncleaf2 * rm_leaf2;
                            mySpecies[s].Nleaf3 -= mySpecies[s].Ncleaf3 * rm_leaf3;
                            mySpecies[s].Ndefoliated += (mySpecies[s].Ncleaf1 * rm_leaf1)
                                                      + (mySpecies[s].Ncleaf2 * rm_leaf2)
                                                      + (mySpecies[s].Ncleaf3 * rm_leaf3);
                        }
                    }
                }
                else if (rm.dm[i].pool == "green" && rm.dm[i].part[j] == "stem")
                {
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        if (dm_stem_green != 0)  //resposibility of other modules to check the amount
                        {
                            double rm_stem = gm2ha * rm.dm[i].dlt[j] * mySpecies[s].dmstem_green / dm_stem_green;
                            double rm_stem1 = rm_stem * MathUtility.Divide(mySpecies[s].dmstem1, mySpecies[s].dmstem_green, 0.0);
                            double rm_stem2 = rm_stem * MathUtility.Divide(mySpecies[s].dmstem2, mySpecies[s].dmstem_green, 0.0);
                            double rm_stem3 = rm_stem * MathUtility.Divide(mySpecies[s].dmstem3, mySpecies[s].dmstem_green, 0.0);
                            mySpecies[s].dmstem1 -= rm_stem1;
                            mySpecies[s].dmstem2 -= rm_stem2;
                            mySpecies[s].dmstem3 -= rm_stem3;
                            mySpecies[s].dmdefoliated += rm_stem1 + rm_stem2 + rm_stem3;

                            mySpecies[s].Nstem1 -= mySpecies[s].Ncstem1 * rm_stem1;
                            mySpecies[s].Nstem2 -= mySpecies[s].Ncstem2 * rm_stem2;
                            mySpecies[s].Nstem3 -= mySpecies[s].Ncstem3 * rm_stem3;
                            mySpecies[s].Ndefoliated += (mySpecies[s].Ncstem1 * rm_stem1)
                                                      + (mySpecies[s].Ncstem2 * rm_stem2)
                                                      + (mySpecies[s].Ncstem3 * rm_stem3);
                        }
                    }
                }
                else if (rm.dm[i].pool == "dead" && rm.dm[i].part[j] == "leaf")
                {
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        if (dm_leaf_dead != 0)  //resposibility of other modules to check the amount
                        {
                            double rm_leaf4 = gm2ha * rm.dm[i].dlt[j] * mySpecies[s].dmleaf4 / dm_leaf_dead;
                            mySpecies[s].dmleaf4 -= rm_leaf4;
                            mySpecies[s].dmdefoliated += rm_leaf4;

                            mySpecies[s].Ndefoliated += mySpecies[s].Ncleaf4 * rm_leaf4;
                            mySpecies[s].Nleaf4 -= mySpecies[s].Ncleaf4 * rm_leaf4;
                        }
                    }
                }
                else if (rm.dm[i].pool == "dead" && rm.dm[i].part[j] == "stem")
                {
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        if (dm_stem_dead != 0)  //resposibility of other modules to check the amount
                        {
                            double rm_stem4 = gm2ha * rm.dm[i].dlt[j] * mySpecies[s].dmstem4 / dm_stem_dead;
                            mySpecies[s].dmstem4 -= rm_stem4;
                            mySpecies[s].dmdefoliated += rm_stem4;

                            mySpecies[s].Nstem4 -= mySpecies[s].Ncstem4 * rm_stem4;
                            mySpecies[s].Ndefoliated += mySpecies[s].Ncstem4 * rm_stem4;
                        }
                    }
                }
            }
        }

        swardHarvestedDM = 0.0;
        swardHarvestedN = 0.0;
        for (int s = 0; s < NumSpecies; s++)
        {
            swardHarvestedDM += mySpecies[s].dmdefoliated;
            swardHarvestedN += mySpecies[s].Ndefoliated;
            mySpecies[s].UpdateAggregated();
            mySpecies[s].UpdatePlantParts();

            // RCichota May 2014: store the defoliated amount (to use for senescence)
            mySpecies[s].prevState.dmdefoliated = mySpecies[s].dmdefoliated;
        }

        //In this routine of no selection among species, the removed tissue from different species
        //will be in proportion with exisisting mass of each species.
        //The digetibility below is an approximation (= that of pasture swards).
        //It is more reasonable to calculate it organ-by-organ for each species, then put them together.
        swardHarvestDigestibility = HerbageDigestibility;

    }

    /// <summary>
    /// Harvest (remove) an amount of plants
    /// </summary>
    /// <param name="type">amount type</param>
    /// <param name="amount">DM amount</param>
    public void Harvest(string type, double amount)
    {
        GrazeType GZ = new GrazeType();
        GZ.amount = (float)amount;
        GZ.type = type;
        OnGraze(GZ);
    }

    /// <summary>
    /// Respond to a Graze event
    /// </summary>
    /// <param name="GZ">Graze</param>
    [EventHandler]
    public void OnGraze(GrazeType GZ)
    {
        if ((!isAlive) || swardShootDM == 0)
            return;

        // get the amount that can potentially be removed
        double AmountRemovable = 0.0;
        for (int s = 0; s < NumSpecies; s++)
            AmountRemovable += Math.Max(0.0, mySpecies[s].dmleaf_green + mySpecies[s].dmstem_green - mySpecies[s].dmgreenmin) + Math.Max(0.0, mySpecies[s].dmdead - mySpecies[s].dmdeadmin);
        AmountRemovable = Math.Max(0.0, AmountRemovable);

        // get the amount required to remove
        double AmountRequired = 0.0;
        if (GZ.type.ToLower() == "SetResidueAmount".ToLower())
        {
            // Remove all DM above given residual amount
            AmountRequired = Math.Max(0.0, StandingPlantWt - GZ.amount);
        }
        else if (GZ.type.ToLower() == "SetRemoveAmount".ToLower())
        {
            // Attempt to remove a given amount
            AmountRequired = Math.Max(0.0, GZ.amount);
        }
        else
        {
            Console.WriteLine("  AgPasture - Method to set amount to remove was not recognized, command will be ignored");
        }
        // get the actual amount to remove
        double AmountToRemove = Math.Min(AmountRequired, AmountRemovable);

        swardHarvestedDM = AmountToRemove;
        swardHarvestedN = 0.0;
        swardHarvestDigestibility = 0.0;

        // get the amounts to remove by species:
        double FractionNotRemoved = 0.0;
        if (AmountRemovable > 0.0)
            FractionNotRemoved = Math.Max(0.0, (AmountRemovable - AmountToRemove) / AmountRemovable);
        double[] TempWeights = new double[NumSpecies];
        double[] TempAmounts = new double[NumSpecies];
        double TempTotal = 0.0;
        if (AmountToRemove > 0.0)
        {
            // get the weights for each species, consider preference and available DM
            double TotalPreference = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                TotalPreference += PreferenceForGreenDM[s] + PreferenceForDeadDM[s];
            for (int s = 0; s < NumSpecies; s++)
            {
                TempWeights[s] = PreferenceForGreenDM[s] + PreferenceForDeadDM[s];
                TempWeights[s] += (TotalPreference - TempWeights[s]) * (1 - FractionNotRemoved);
                TempAmounts[s] = Math.Max(0.0, mySpecies[s].dmleaf_green + mySpecies[s].dmstem_green - mySpecies[s].dmgreenmin) + Math.Max(0.0, mySpecies[s].dmdead - mySpecies[s].dmdeadmin);
                TempTotal += TempAmounts[s] * TempWeights[s];
            }

            // get the actual amounts to remove for each species
            for (int s = 0; s < NumSpecies; s++)
            {
                if (TempTotal > 0.0)
                    FractionToHarvest[s] = Math.Max(0.0, Math.Min(1.0, TempWeights[s] * TempAmounts[s] / TempTotal));
                else
                    FractionToHarvest[s] = 0.0;
                swardHarvestedN += mySpecies[s].RemoveDM(AmountToRemove * FractionToHarvest[s], PreferenceForGreenDM[s], PreferenceForDeadDM[s]);

                // get digestibility of harvested material
                swardHarvestDigestibility += mySpecies[s].digestDefoliated * mySpecies[s].dmdefoliated / AmountToRemove;
            }
        }
    }

    /// <summary>
    /// Respond to a Reset event
    /// </summary>
    [EventHandler]
    public void OnReset()
    {

        // set all species to their initial state (DM, N, LAI, etc.)
        for (int s = 0; s < NumSpecies; s++)
        {
            if (!usingSpeciesRoot)
            {  // Get the max root depth of all species
                InitialState[s].RootDepth = iniRootDepth.Max();
            }

            SetSpeciesState(s, InitialState[s]);
        }
    }

    /// <summary>
    /// Allow setting up the DM and N amounts of any species
    /// </summary>
    /// <param name="NewSetState">New set of values for DM and N of given species</param>
    /// <remarks>
    /// The type NewSetState contains:
    ///   - species: List with the index of species to be changed
    ///   - dmShoot: Array of DM shoot values for each species being changed
    ///   - dmRoot: Array of DM root values for each species being changed
    ///   - rootDepth: Array of root depth values for each species being changed
    ///   - dmFractions: Array with values of DM fractions for each pool in each species being changed
    ///   - nConcentrations: Array of N concentration values for each pool in each species being changed
    /// </remarks>
    [EventHandler]
    public void OnSetSpeciesState(SetSpeciesStateType NewSetState)
    {
        // in theory all parameters are optional, but dmFractions and nConcentrations need to be initialised or APSIM crashes

        SpeciesStateSettings NewState = new SpeciesStateSettings();
        NewState.DMFraction = new double[11];
        NewState.NConcentration = new double[12];

        foreach (int s in NewSetState.speciesID)
        {
            // get DM shoot
            if (NewSetState.dmShoot.Length > 0)
                NewState.ShootDM = NewSetState.dmShoot[s];
            else
                NewState.ShootDM = mySpecies[s].dmshoot;

            // get DM root
            if (NewSetState.dmRoot.Length > 0)
                NewState.RootDM = NewSetState.dmRoot[s];
            else
                NewState.RootDM = mySpecies[s].dmroot;

            // get root depth
            if (NewSetState.rootDepth.Length > 0)
                NewState.RootDepth = rootDepth[s];
            else
                NewState.RootDepth = mySpecies[s].rootDepth;

            // get dm fractions
            if (NewSetState.dmFractions.Length >0)
            {
                NewState.DMFraction[0] = NewSetState.dmFractions[s].Leaf1;
                NewState.DMFraction[1] = NewSetState.dmFractions[s].Leaf2;
                NewState.DMFraction[2] = NewSetState.dmFractions[s].Leaf3;
                NewState.DMFraction[3] = NewSetState.dmFractions[s].Leaf4;
                NewState.DMFraction[4] = NewSetState.dmFractions[s].Stem1;
                NewState.DMFraction[5] = NewSetState.dmFractions[s].Stem2;
                NewState.DMFraction[6] = NewSetState.dmFractions[s].Stem3;
                NewState.DMFraction[7] = NewSetState.dmFractions[s].Stem4;
                NewState.DMFraction[8] = NewSetState.dmFractions[s].Stolon1;
                NewState.DMFraction[9] = NewSetState.dmFractions[s].Stolon2;
                NewState.DMFraction[10] = NewSetState.dmFractions[s].Stolon3;
                // NewSetState.dmFractions[s].Roots is not used here
            }
            else
            {
                NewState.DMFraction[0] = mySpecies[s].dmleaf1 / mySpecies[s].dmshoot;
                NewState.DMFraction[1] = mySpecies[s].dmleaf2 / mySpecies[s].dmshoot;
                NewState.DMFraction[2] = mySpecies[s].dmleaf3 / mySpecies[s].dmshoot;
                NewState.DMFraction[3] = mySpecies[s].dmleaf4 / mySpecies[s].dmshoot;
                NewState.DMFraction[4] = mySpecies[s].dmstem1 / mySpecies[s].dmshoot;
                NewState.DMFraction[5] = mySpecies[s].dmstem2 / mySpecies[s].dmshoot;
                NewState.DMFraction[6] = mySpecies[s].dmstem3 / mySpecies[s].dmshoot;
                NewState.DMFraction[7] = mySpecies[s].dmstem4 / mySpecies[s].dmshoot;
                NewState.DMFraction[8] = mySpecies[s].dmstol1 / mySpecies[s].dmshoot;
                NewState.DMFraction[9] = mySpecies[s].dmstol2 / mySpecies[s].dmshoot;
                NewState.DMFraction[10] = mySpecies[s].dmstol3 / mySpecies[s].dmshoot;
            }

            // get N concentrations
            if (NewSetState.nConcentrations.Length>0)
            {
                NewState.NConcentration[0] = NewSetState.nConcentrations[s].Leaf1;
                NewState.NConcentration[1] = NewSetState.nConcentrations[s].Leaf2;
                NewState.NConcentration[2] = NewSetState.nConcentrations[s].Leaf3;
                NewState.NConcentration[3] = NewSetState.nConcentrations[s].Leaf4;
                NewState.NConcentration[4] = NewSetState.nConcentrations[s].Stem1;
                NewState.NConcentration[5] = NewSetState.nConcentrations[s].Stem2;
                NewState.NConcentration[6] = NewSetState.nConcentrations[s].Stem3;
                NewState.NConcentration[7] = NewSetState.nConcentrations[s].Stem4;
                NewState.NConcentration[8] = NewSetState.nConcentrations[s].Stolon1;
                NewState.NConcentration[9] = NewSetState.nConcentrations[s].Stolon2;
                NewState.NConcentration[10] = NewSetState.nConcentrations[s].Stolon3;
                NewState.NConcentration[11] = NewSetState.nConcentrations[s].Roots;
            }
            else
            {
                NewState.NConcentration[0] = mySpecies[s].Ncleaf1;
                NewState.NConcentration[1] = mySpecies[s].Ncleaf2;
                NewState.NConcentration[2] = mySpecies[s].Ncleaf3;
                NewState.NConcentration[3] = mySpecies[s].Ncleaf4;
                NewState.NConcentration[4] = mySpecies[s].Ncstem1;
                NewState.NConcentration[5] = mySpecies[s].Ncstem2;
                NewState.NConcentration[6] = mySpecies[s].Ncstem3;
                NewState.NConcentration[7] = mySpecies[s].Ncstem4;
                NewState.NConcentration[8] = mySpecies[s].Ncstol1;
                NewState.NConcentration[9] = mySpecies[s].Ncstol2;
                NewState.NConcentration[10] = mySpecies[s].Ncstol3;
                NewState.NConcentration[11] = mySpecies[s].Ncroot;
            }

            // Set the species
            SetSpeciesState(s, NewState);
        }
    }

    #endregion  -----------------------------------------------------------------------------------

    #region Functions  ----------------------------------------------------------------------------

    /// <summary>
    /// Compute the distribution of roots in the soil profile (sum is equal to one)
    /// </summary>
    /// <returns>The proportion of root mass in each soil layer</returns>
    /// <param name="s">The index of the species to compute root distribution</param>
    private double[] RootProfileDistribution(int s)
    {
        int nLayers = dlayer.Length;
        double[] result = new double[nLayers];
        double sumProportion = 0.0;
        double myRootDepth = 0.0;
        if (s >= 0)
        {
            myRootDepth = mySpecies[s].rootDepth;
        }
        else
        {
            myRootDepth = swardRootDepth;
            s = 0;   // will use parameters from first species
        }

        switch (p_RootDistributionMethod)
        {
            case 0:
                {
                    // homogenous distribution over soil profile (same root density throughout the profile)
                    double DepthTop = 0.0;
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        if (DepthTop >= myRootDepth)
                            result[layer] = 0.0;
                        else if (DepthTop + dlayer[layer] <= myRootDepth)
                            result[layer] = dlayer[layer];
                        else
                            result[layer] = myRootDepth - DepthTop;
                        sumProportion += result[layer];
                        DepthTop += dlayer[layer];
                    }
                    break;
                }
            case 1:
                {
                    // distribution given by the user
                    Array.Resize(ref rlvp, nLayers);    // This will remove values in excess (non-existing layers) or add zeroes if layers are missing
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        result[layer] = rlvp[layer];
                        sumProportion += result[layer];
                    }
                    break;
                }
            case 2:
                {
                    // distribution calculated using ExpoLinear method
                    //  Considers homogeneous distribution from surface down to a fraction of root depth (p_ExpoLinearDepthParam)
                    //   below this depth, the proportion of root decrease following a power function (exponent = p_ExpoLinearCurveParam)
                    //   if exponent is one than the proportion decreases linearly.
                    double DepthTop = 0.0;
                    double DepthFirstStage = myRootDepth * p_ExpoLinearDepthParam[s];
                    double DepthSecondStage = myRootDepth - DepthFirstStage;
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        if (DepthTop >= myRootDepth)
                            result[layer] = 0.0;
                        else if (DepthTop + dlayer[layer] <= DepthFirstStage)
                            result[layer] = dlayer[layer];
                        else
                        {
                            if (DepthTop < DepthFirstStage)
                                result[layer] = DepthFirstStage - DepthTop;
                            if ((p_ExpoLinearDepthParam[s] < 1.0) && (p_ExpoLinearCurveParam[s] > 0.0))
                            {
                                double thisDepth = Math.Max(0.0, DepthTop - DepthFirstStage);
                                double Ftop = (thisDepth - DepthSecondStage) / (p_ExpoLinearCurveParam[s] + 1);
                                if (DepthSecondStage > 0.0)
                                    Ftop *= Math.Pow(1 - (thisDepth / DepthSecondStage), p_ExpoLinearCurveParam[s]);
                                thisDepth = Math.Min(DepthTop + dlayer[layer] - DepthFirstStage, DepthSecondStage);
                                double Fbottom = (thisDepth - DepthSecondStage) / (p_ExpoLinearCurveParam[s] + 1);
                                if (DepthSecondStage > 0.0)
                                    Fbottom *= Math.Pow(1 - (thisDepth / DepthSecondStage), p_ExpoLinearCurveParam[s]);
                                result[layer] += Math.Max(0.0, Fbottom - Ftop);
                            }
                            else if (DepthTop + dlayer[layer] <= myRootDepth)
                            {
                                result[layer] += Math.Min(DepthTop + dlayer[layer], myRootDepth)
                                              - Math.Max(DepthTop, DepthFirstStage);
                            }
                        }

                        sumProportion += result[layer];
                        DepthTop += dlayer[layer];
                    }
                    break;
                }
            default:
                {
                    throw new Exception("No valid method for computing root distribution was selected");
                }
        }
        if (sumProportion > 0.0)
            for (int layer = 0; layer < nLayers; layer++)
                result[layer] = result[layer] / sumProportion;
        else
            throw new Exception("Could not calculate root distribution");
        return result;
    }

    /// <summary>
    /// Compute how much of the layer is actually explored by roots
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="root_depth"></param>
    /// <returns>Fraction of layer explored by roots</returns>
    private double LayerFractionForRoots(int layer, double root_depth)
    {
        double depthToTopOfLayer = 0.0;
        double fraction_in_layer = 0.0;
        for (int i = 0; i < layer; i++)
            depthToTopOfLayer += dlayer[i];
        fraction_in_layer = (root_depth - depthToTopOfLayer) / dlayer[layer];

        return Math.Min(1.0, Math.Max(0.0, fraction_in_layer));
    }

    /// <summary>
    /// Plant height calculation from DM
    /// </summary>
    private double HeightfromDM()
    {
        if (usingPairWise)
        {  // as implemented by Frank Li
            double ht = HeightMassFN.Value(swardGreenDM + swardDeadDM);
            if (ht < 20.0) ht = 20.0;      // minimum = 20mm
            return ht;
        }
        else
        {
            double TodaysHeight = MaxPlantHeight[0];

            if ((swardGreenDM + swardDeadDM) <= MassForMaxHeight[0])
            {
                double myX = MathUtility.Divide(swardGreenDM + swardDeadDM, MassForMaxHeight[0], 0.0);
                double heightF = ExponentHeightFromMass[0]
                               - (ExponentHeightFromMass[0] * myX)
                               + myX;
                heightF *= Math.Pow(myX, ExponentHeightFromMass[0] - 1);
                TodaysHeight *= heightF;
            }

            return Math.Max(TodaysHeight, MinimumHeight);
        }
    }

    /// <summary>
    /// The following helper functions [VDP and svp] are for calculating Fvdp
    /// </summary>
    /// <returns></returns>
    private double VPD()
    {
        double SVPfrac = 0.66F;
        double VPDmint = svp(MetData.MinT) - MetData.vp;
        VPDmint = Math.Max(VPDmint, 0.0);

        double VPDmaxt = svp(MetData.MaxT) - MetData.vp;
        VPDmaxt = Math.Max(VPDmaxt, 0.0);

        double vdp = (SVPfrac * VPDmaxt)
                   + ((1 - SVPfrac) * VPDmint);
        return vdp;
    }

    private double svp(double temp)  // from Growth.for documented in MicroMet
    {
        return 6.1078 * Math.Exp(17.269 * temp / (237.3 + temp));
    }

    #endregion  -----------------------------------------------------------------------------------

    #region Output properties  --------------------------------------------------------------------

    /// <summary>An output</summary>
    [Output]
    [Description("Generic type of crop")]         //  useful for SWIM
    [Units("")]
    public string Crop_type
    {
        get { return thisCropName; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Name of this crop")]
    [Units("")]
    public string Crop_name
    {
        get { return thisCropName; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Generic crop type of each species")]
    [Units("")]
    public string[] SpeciesCrop_type
    {
        get { return micrometType; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Name of each species")]
    [Units("")]
    public string[] Species_name
    {
        get
        {
            string[] result = new string[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].speciesName;
            return result;
        }
    }

    private bool isAlive = true;
    /// <summary>An output</summary>
    [Output]
    [Description("Flag signalling whether plants are alive")]
    [Units("true/false")]
    public bool IsAlive
    {
        get
        {
            return isAlive;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant status (dead, alive, etc.)")]
    [Units("")]
    public string plant_status
    {
        get
        {
            if (isAlive) return "alive";
            else return "out";
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant development stage number")]
    [Units("")]
    public int Stage
    {
        //An approximate of the stages corresponding to that of other arable crops for management application settings.
        //Phenostage of the first species (ryegrass) is used for this approximation
        get
        {
            int cropStage = 0; //default as "phase out"
            if (isAlive)
            {
                if (mySpecies[0].phenoStage == 0)
                    cropStage = 1;    //"sowing & germination";
                if (mySpecies[0].phenoStage == 1)
                    cropStage = 3;    //"emergence";
            }
            return cropStage;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant development stage name")]
    [Units("")]
    public string StageName
    {
        get
        {
            string name = "out";
            if (isAlive)
            {
                if (mySpecies[0].phenoStage == 0)
                    name = "sowing";    //cropStage = 1 & 2
                if (mySpecies[0].phenoStage == 1)
                    name = "emergence"; // cropStage = 3
            }
            return name;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total amount of C in plants")]
    [Units("kgDM/ha")]
    public double TotalPlantC
    {
        get { return 0.4 * (swardShootDM + swardRootDM); }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plants")]
    [Units("kgDM/ha")]
    public double TotalPlantWt
    {
        get { return AboveGroundWt + BelowGroundWt; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plants above ground")]
    [Units("kgDM/ha")]
    public double AboveGroundWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmshoot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plants below ground")]
    [Units("kgDM/ha")]
    public double BelowGroundWt
    {
        get { return swardRootDM; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of standing plants parts")]
    [Units("kgDM/ha")]
    public double StandingPlantWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmleaf + mySpecies[s].dmstem;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plants alive above ground")]
    [Units("kgDM/ha")]
    public double AboveGroundLiveWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmgreen;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of dead plants above ground")]
    [Units("kgDM/ha")]
    public double AboveGroundDeadWt
    {
        get { return swardDeadDM; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's leaves")]
    [Units("kgDM/ha")]
    public double LeafWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmleaf1 + mySpecies[s].dmleaf2 + mySpecies[s].dmleaf3 + mySpecies[s].dmleaf4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's leaves alive")]
    [Units("kgDM/ha")]
    public double LeafLiveWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmleaf1 + mySpecies[s].dmleaf2 + mySpecies[s].dmleaf3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's leaves dead")]
    [Units("kgDM/ha")]
    public double LeafDeadWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmleaf4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's stems")]
    [Units("kgDM/ha")]
    public double StemWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmstem1 + mySpecies[s].dmstem2 + mySpecies[s].dmstem3 + mySpecies[s].dmstem4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's stems alive")]
    [Units("kgDM/ha")]
    public double StemLiveWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmstem1 + mySpecies[s].dmstem2 + mySpecies[s].dmstem3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's stems dead")]
    [Units("kgDM/ha")]
    public double StemDeadWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmstem4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's stolons")]
    [Units("kgDM/ha")]
    public double StolonWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmstol1 + mySpecies[s].dmstol2 + mySpecies[s].dmstol3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's roots")]
    [Units("kgDM/ha")]
    public double RootWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmroot;
            return result;
        }
    }

    //for consistency, passing variables in Onremove_crop_biomass() similar with other plant modules
    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plants above ground")]
    [Units("kg/ha")]
    public double biomass { get { return AboveGroundWt; } }
    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's leaves alive")]
    [Units("g/m^2")]
    public double leafgreenwt { get { return LeafLiveWt * 0.10; } }
    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's leaves dead")]
    [Units("g/m^2")]
    public double stemgreenwt { get { return StemLiveWt * 0.10; } }
    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's stems dead")]
    [Units("g/m^2")]
    public double leafsenescedwt { get { return LeafDeadWt * 0.10; } }
    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plant's stems alive")]
    [Units("g/m^2")]
    public double stemsenescedwt { get { return StemDeadWt * 0.10; } }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant potential carbon assimilation")]
    [Units("kgC/ha")]
    public double PlantPotentialCarbonAssimilation
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Pgross;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant base potential photosynthetic rate")]
    [Units("kgC/ha")]
    public double PlantPotentialPhotosynthesis
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].PotPhoto;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant carbon loss by respiration")]
    [Units("kgC/ha")]
    public double PlantCarbonLossRespiration
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Resp_m + mySpecies[s].Resp_g;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant gross potential growth")]
    [Units("kgDM/ha")]
    public double PlantPotentialGrossGrowth
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Pgross / CarbonFractionDM;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Potential plant growth, correct for extreme temperatures")]
    [Units("kgDM/ha")]
    public double PlantPotentialGrowthWt
    {
        get { return swardPotentialGrowth; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Potential plant growth, correct for temperature and water")]
    [Units("kgDM/ha")]
    public double PlantGrowthNoNLimit
    {
        get { return swardPotGrowthWater; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Actual plant growth (before littering)")]
    [Units("kgDM/ha")]
    public double PlantGrowthWt
    {
        //dm_daily_growth, including roots & before littering
        get { return swardActualGrowth; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Actual herbage (shoot) growth")]
    [Units("kgDM/ha")]
    public double HerbageGrowthWt
    {
        get { return swardHerbageGrowth; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant effective growth (actual minus tissue turnover)")]
    [Units("kgDM/ha")]
    public double PlantEffectiveGrowthWt
    {
        get { return swardActualGrowth - swardLitterDM - swardSenescedRootDM; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter amount of litter deposited onto soil surface")]
    [Units("kgDM/ha")]
    public double LitterDepositionWt
    {
        get { return swardLitterDM; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter amount of senescent roots added to soil FOM")]
    [Units("kgDM/ha")]
    public double RootSenescenceWt
    {
        get { return swardSenescedRootDM; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant C remobilisation")]
    [Units("kgC/ha")]
    public double PlantRemobilisedC
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Cremob;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter amount available for removal (leaf+stem)")]
    [Units("kgDM/ha")]
    public double HarvestableWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += Math.Max(0.0, mySpecies[s].dmleaf_green + mySpecies[s].dmstem_green - mySpecies[s].dmgreenmin)
                        + Math.Max(0.0, mySpecies[s].dmdead - mySpecies[s].dmdeadmin);
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of plant dry matter removed by harvest")]
    [Units("kgDM/ha")]
    public double HarvestWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmdefoliated;
            return result;
        }
    }

    //**LAI & Cover
    /// <summary>An output</summary>
    [Output]
    [Description("Leaf area index of green leaves")]
    [Units("m^2/m^2")]
    public double LAI_green
    {
        get { return swardGreenLAI; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Leaf area index of dead leaves")]
    [Units("m^2/m^2")]
    public double LAI_dead
    {
        get { return swardDeadLAI; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total leaf area index")]
    [Units("m^2/m^2")]
    public double LAI_total
    {
        get { return swardTotalLAI; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Fraction of soil covered by green leaves")]
    [Units("0-1")]
    public double Cover_green
    {
        get
        {
            if (swardGreenLAI == 0) return 0;
            return 1.0 - Math.Exp(-swardLightExtCoeff * swardGreenLAI);
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Fraction of soil covered by dead leaves")]
    [Units("0-1")]
    public double Cover_dead
    {
        get
        {
            if (swardDeadLAI == 0) return 0;
            return 1.0 - Math.Exp(-swardLightExtCoeff * swardDeadLAI);
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Fraction of soil covered by plants")]
    [Units("0-1")]
    public double Cover_tot
    {
        get
        {
            if (swardTotalLAI == 0) return 0;
            return 1.0 - (Math.Exp(-swardLightExtCoeff * swardTotalLAI));
        }
    }

    //** Nitrogen
    /// <summary>An output</summary>
    [Output]
    [Description("Total amount of N in plants")]
    [Units("kg/ha")]
    public double TotalPlantN
    {
        get { return AboveGroundN + BelowGroundN; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total amount of N in plants above ground")]
    [Units("kgN/ha")]
    public double AboveGroundN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nshoot;       //remobilised N is reported in stem
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total amount of N in plants below ground")]
    [Units("kgN/ha")]
    public double BelowGroundN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nroot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Proportion of N above ground in relation to below ground")]
    [Units("%")]
    public double AboveGroundNPct
    {
        get
        {
            double result = 0.0;
            if (AboveGroundWt != 0)
                result = 100 * AboveGroundN / AboveGroundWt;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total amount of N in standing plants")]
    [Units("kgN/ha")]
    public double StandingPlantN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nleaf + mySpecies[s].Nstem;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average N concentration of standing plants")]
    [Units("kgN/kgDM")]
    public double StandingPlantNConc
    {
        get
        {
            double Namount = 0.0;
            double DMamount = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                Namount += mySpecies[s].Nleaf + mySpecies[s].Nstem;
                DMamount += mySpecies[s].dmleaf + mySpecies[s].dmstem;
            }
            double result = MathUtility.Divide(Namount, DMamount, 0.0);
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total amount of N in plants alive above ground")]
    [Units("kgN/ha")]
    public double AboveGroundLiveN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Ngreen;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total amount of N in dead plants above ground")]
    [Units("kgN/ha")]
    public double AboveGroundDeadN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Ndead;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total amount of N in the plant's leaves")]
    [Units("kgN/ha")]
    public double LeafN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nleaf;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total amount of N in the plant's stems")]
    [Units("kgN/ha")]
    public double StemN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nstem;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total amount of N in the plant's stolons")]
    [Units("kgN/ha")]
    public double StolonN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nstolon;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total amount of N in the plant's roots")]
    [Units("kgN/ha")]
    public double RootN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nroot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average N concentration of leaves")]
    [Units("kgN/kgDM")]
    public double LeafNConc
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += (mySpecies[s].Ncleaf1 * mySpecies[s].dmleaf1)
                        + (mySpecies[s].Ncleaf2 * mySpecies[s].dmleaf2)
                        + (mySpecies[s].Ncleaf3 * mySpecies[s].dmleaf3)
                        + (mySpecies[s].Ncleaf4 * mySpecies[s].dmleaf4);
            if (result > 0.0)
                result /= LeafWt;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average N concentration in stems")]
    [Units("kgN/kgDM")]
    public double StemNConc
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += (mySpecies[s].Ncstem1 * mySpecies[s].dmstem1)
                        + (mySpecies[s].Ncstem2 * mySpecies[s].dmstem2)
                        + (mySpecies[s].Ncstem3 * mySpecies[s].dmstem3)
                        + (mySpecies[s].Ncstem4 * mySpecies[s].dmstem4);
            if (result > 0.0)
                result /= StemWt;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average N concentration in stolons")]
    [Units("kgN/kgDM")]
    public double StolonNConc
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += (mySpecies[s].Ncstol1 * mySpecies[s].dmstol1)
                        + (mySpecies[s].Ncstol2 * mySpecies[s].dmstol2)
                        + (mySpecies[s].Ncstol3 * mySpecies[s].dmstol3);
            if (result > 0.0)
                result /= StolonWt;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average N concentration in roots")]
    [Units("kgN/kgDM")]
    public double RootNConc
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Ncroot * mySpecies[s].dmroot;
            if (result > 0.0)
                result /= RootWt;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N removed by harvest")]
    [Units("kgN/ha")]
    public double HarvestN
    {
        get
        {
            double result = 0.0;
            if (HarvestWt > 0.0)
            {
                for (int s = 0; s < NumSpecies; s++)
                    result += mySpecies[s].Ndefoliated;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average herbage digestibility")]
    [Units("0-1")]
    public double HerbageDigestibility
    {
        get
        {
            if (!isAlive || (StemWt + LeafWt) <= 0)
                return 0.0;

            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].digestHerbage * (mySpecies[s].dmstem + mySpecies[s].dmleaf) / (StemWt + LeafWt);
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average digestibility of harvested material")]
    [Units("0-1")]
    public double DefoliatedDigestibility
    {
        get { return swardHarvestDigestibility; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average ME of herbage")]
    [Units("(MJ/ha)")]
    public double HerbageME
    {
        get
        {
            double result = 16 * HerbageDigestibility * (StemWt + LeafWt);
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average ME concentration of herbage")]
    [Units("(MJ/kgDM)")]
    public double HerbageMEconc
    {
        get { return 16 * HerbageDigestibility; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of atmospheric N fixed")]
    [Units("kgN/ha")]
    public double PlantFixedN
    {
        get { return swardNFixed; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N from senescing tissue potentially remobilisable")]
    [Units("kgN/ha")]
    public double PlantRemobilisableSenescedN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nremob;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N remobilised from senescing tissue")]
    [Units("kgN/ha")]
    public double PlantRemobilisedN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].remob2NewGrowth;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of luxury N remobilised")]
    [Units("kgN/ha")]
    public double PlantLuxuryNRemobilised
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].NFastRemob2 + mySpecies[s].NFastRemob3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of luxury N potentially remobilisable")]
    [Units("kgN/ha")]
    public double PlantRemobilisableLuxuryN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].NLuxury2 + mySpecies[s].NLuxury3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N deposited as litter onto soil surface")]
    [Units("kgN/ha")]
    public double LitterDepositionN
    {
        get { return swardLitterN; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N added to soil FOM by senescent roots")]
    [Units("kgN/ha")]
    public double RootSenescenceN
    {
        get { return swardSenescedRootN; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant nitrogen requirement with luxury uptake")]
    [Units("kgN/ha")]
    public double NitrogenRequiredLuxury
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                result += mySpecies[s].NdemandLux;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant nitrogen requirement for optimum growth")]
    [Units("kgN/ha")]
    public double NitrogenRequiredOptimum
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                result += mySpecies[s].NdemandOpt;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Nitrogen amount in new growth")]
    [Units("kgN/ha")]
    public double PlantGrowthN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                result += mySpecies[s].newGrowthN;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Nitrogen concentration in new growth")]
    [Units("-")]
    public double PlantGrowthNconc
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                result += mySpecies[s].newGrowthN;
            }
            if (PlantGrowthWt > 0.0)
                result /= PlantGrowthWt;
            else
                result = 0.0;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant nitrogen demand from soil")]
    [Units("kgN/ha")]
    public double NitrogenDemand
    {
        get { return swardSoilNDemand; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant available nitrogen in soil")]
    [Units("kgN/ha")]
    public double NitrogenSupply
    {
        get { return swardSoilNavailable; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant available nitrogen in soil layers")]
    [Units("kgN/ha")]
    public double[] NitrogenSupplyLayers
    {
        get
        {
            double[] result = new double[dlayer.Length];
            for (int layer = 0; layer < dlayer.Length; layer++)
                result[layer] = soilNH4Available[layer] + soilNO3Available[layer];
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant nitrogen uptake")]
    [Units("kgN/ha")]
    public double NitrogenUptake
    {
        get { return soilNH4Uptake.Sum() + soilNO3Uptake.Sum(); }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant nitrogen uptake from soil layers")]
    [Units("kgN/ha")]
    public double[] NitrogenUptakeLayers
    {
        get
        {
            double[] result = new double[dlayer.Length];
            for (int layer = 0; layer < dlayer.Length; layer++)
                result[layer] = soilNH4Uptake[layer] + soilNO3Uptake[layer];
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant growth limiting factor due to nitrogen stress")]
    [Units("0-1")]
    public double GLFn
    {
        get { return swardGLFN; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant growth limiting factor due to plant N concentration")]
    [Units("0-1")]
    public double GLFnConcentration
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].NcFactor * mySpecies[s].dmshoot;
            if (AboveGroundWt > 0.0)
                result /= AboveGroundWt;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter allocated to roots")]
    [Units("kgDM/ha")]
    public double DMToRoots
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                result += (1 - mySpecies[s].fShoot) * mySpecies[s].dGrowth;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter allocated to shoot")]
    [Units("kgDM/ha")]
    public double DMToShoot
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                result += mySpecies[s].fShoot * mySpecies[s].dGrowth;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Fraction of growth allocated above ground (fshoot)")]
    [Units("0-1")]
    public double FractionGrowthToShoot
    {
        get
        {
            double result = 0.0;
            if (swardActualGrowth > 0.0)
                result = DMToShoot / swardActualGrowth;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Fraction of growth allocated to roots")]
    [Units("0-1")]
    public double FractionGrowthToRoot
    {
        get
        {
            double result = 0.0;
            if (swardActualGrowth > 0.0)
                result = DMToRoots / swardActualGrowth;
            return result;
        }
    }

    //** water related
    /// <summary>An output</summary>
    [Output]
    [Description("Root length density")]
    [Units("mm/mm^3")]
    public double[] rlv
    {
        get
        {
            //Compute the root length, total over the whole profile
            double[] result = new double[dlayer.Length];
            double Total_Rlength = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {  // average root length (m root/m2 soil)
                Total_Rlength += (mySpecies[s].dmroot * 0.1) * mySpecies[s].specificRootLength;
            }
            Total_Rlength *= 0.001;          // convert into mm root/mm2 soil)
            for (int layer = 0; layer < result.Length; layer++)
            {
                result[layer] = RootFraction[layer] * Total_Rlength / dlayer[layer];    // mm root/mm3 soil
            }
            return result;
        }
    }

    private double[] RootFraction;
    /// <summary>An output</summary>
    [Output]
    [Description("Fraction of root dry matter for each soil layer")]
    [Units("0-1")]
    public double[] RootWtFraction
    {
        get { return RootFraction; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant water demand")]
    [Units("mm")]
    public double WaterDemand
    {
        get { return swardWaterDemand; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant available water in soil")]
    [Units("mm")]
    public double WaterSupply
    {
        get { return soilAvailableWater.Sum(); }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant available water in soil layers")]
    [Units("mm")]
    public double[] WaterSupplyLayers
    {
        get { return soilAvailableWater; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant water uptake")]
    [Units("mm")]
    public double WaterUptake
    {
        get { return soilWaterUptake.Sum(); }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant water uptake from soil layers")]
    [Units("mm")]
    public double[] WaterUptakeLayers
    {
        get { return soilWaterUptake; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant growth limiting factor due to water deficit")]
    [Units("0-1")]
    public double GLFwater
    {
        get { return swardGLFWater; }
    }

    //**Stress factors
    /// <summary>An output</summary>
    [Output]
    [Description("Plant growth limiting factor due to temperature")]
    [Units("0-1")]
    public double GLFtemp
    {
        get { return swardGLFTemp; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Generic plant growth limiting factor related to soil fertility, user set")]
    [Units("0-1")]
    public double GLFsfert
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                double prop = 1.0 / NumSpecies;
                if (swardGreenDM > 0.0)
                {
                    prop = mySpecies[s].dmgreen / swardGreenDM;
                }
                result += mySpecies[s].GLFSFertility * prop;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Generic plant growth limiting factor, user set")]
    [Units("0-1")]
    public double GLFgeneric
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                double prop = 1.0 / NumSpecies;
                if (swardGreenDM > 0.0)
                {
                    prop = mySpecies[s].dmgreen / swardGreenDM;
                }
                result += mySpecies[s].GLFGeneric * prop;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant growth limiting factor due to extreme temperatures")]
    [Units("0-1")]
    public double GLFxtemp
    {
        get
        {
            double result = 1.0;
            if (PlantPotentialPhotosynthesis > 0.0)
            {
                result = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                    result += mySpecies[s].Pgross * mySpecies[s].ExtremeTempStress;
                result /= PlantPotentialPhotosynthesis;
            }
            return result;
        }
    }

    private double frgr = 1.0;
    /// <summary>An output</summary>
    [Output]
    [Description("Plant relative growth rate, sent to micromet")]
    [Units("0-1")]
    public double Frgr
    {
        get
        {
            return frgr;
        }
    }
    /// <summary>An output</summary>
    [Output]
    [Description("Sward average height")]                 //needed by micromet
    [Units("mm")]
    public double Height
    {
        get { return swardHeight; }
    }

    //testing purpose
    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter of plant pools at stage 1 (young)")]
    [Units("kgN/ha")]
    public double PlantStage1Wt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmleaf1 + mySpecies[s].dmstem1 + mySpecies[s].dmstol1;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter of plant pools at stage 2 (developing)")]
    [Units("kgN/ha")]
    public double PlantStage2Wt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmleaf2 + mySpecies[s].dmstem2 + mySpecies[s].dmstol2;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter of plant pools at stage 3 (mature)")]
    [Units("kgN/ha")]
    public double PlantStage3Wt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmleaf3 + mySpecies[s].dmstem3 + mySpecies[s].dmstol3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter of plant pools at stage 4 (senescent)")]
    [Units("kgN/ha")]
    public double PlantStage4Wt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmleaf4 + mySpecies[s].dmstem4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N content of plant pools at stage 1 (young)")]
    [Units("kgN/ha")]
    public double PlantStage1N
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nleaf1 + mySpecies[s].Nstem1 + mySpecies[s].Nstol1;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N content of plant pools at stage 2 (developing)")]
    [Units("kgN/ha")]
    public double PlantStage2N
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nleaf2 + mySpecies[s].Nstem2 + mySpecies[s].Nstol2;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N content of plant pools at stage 3 (mature)")]
    [Units("kgN/ha")]
    public double PlantStage3N
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nleaf3 + mySpecies[s].Nstem3 + mySpecies[s].Nstol3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N content of plant pools at stage 4 (senescent)")]
    [Units("kgN/ha")]
    public double PlantStage4N
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Nleaf4 + mySpecies[s].Nstem4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Vapour pressure deficit")]
    [Units("kPa")]
    public double VPD_out              // VPD effect on Growth Interpolation Set
    {
        get { return VPD(); }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Effect of vapour pressure on growth (used by micromet)")]
    [Units("0-1")]
    public double FVPD              // VPD effect on Growth Interpolation Set
    {                               // mostly = 1 for crop/grass/forage
        get { return FVPDFunction.Value(VPD()); }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Solar radiation intercepted by whole sward")]
    [Units("MJ")]
    public double InterceptedRadn;

    //// The following are species values (arrays) ------------------------------------------------

    /// <summary>An output</summary>
    [Output]
    [Description("Leaf area index of green leaves, for each species")]
    [Units("m^2/m^2")]
    public double[] SpeciesGreenLAI
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].greenLAI;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Leaf area index of dead leaves, for each species")]
    [Units("m^2/m^2")]
    public double[] SpeciesDeadLAI
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].deadLAI;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total leaf area index, for each species")]
    [Units("m^2/m^2")]
    public double[] SpeciesTotalLAI
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].totalLAI;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Fraction of soil covered by green leaves, for each species")]
    [Units("0-1")]
    public double[] SpeciesGreenCover
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].coverGreen;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Fraction of soil covered by dead leaves, for each species")]
    [Units("0-1")]
    public double[] SpeciesDeadCover
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].coverDead;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Fraction of soil covered by plant, for each species")]
    [Units("0-1")]
    public double[] SpeciesTotalCover
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].coverTotal;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total dry matter weight of plants for each plant species")]
    [Units("kgDM/ha")]
    public double[] SpeciesTotalWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmshoot + mySpecies[s].dmroot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of plants above ground, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesAboveGroundWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmshoot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of plants below ground, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesBelowGroundWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmroot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of standing herbage, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesStandingWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmleaf + mySpecies[s].dmstem;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of live standing plants parts for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesStandingLiveWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmleaf_green + mySpecies[s].dmstem_green;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of dead standing plants parts for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesStandingDeadWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmleaf4 + mySpecies[s].dmstem4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of leaves for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmleaf1 + mySpecies[s].dmleaf2 + mySpecies[s].dmleaf3 + mySpecies[s].dmleaf4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of stems for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStemWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmstem1 + mySpecies[s].dmstem2 + mySpecies[s].dmstem3 + mySpecies[s].dmstem4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of stolons for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmstol1 + mySpecies[s].dmstol2 + mySpecies[s].dmstol3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of roots for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRootWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmroot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Total N amount for each plant species")]
    [Units("kgN/ha")]
    public double[] SpeciesTotalN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nshoot + mySpecies[s].Nroot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount of standing herbage, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStandingN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nleaf + mySpecies[s].Nstem;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in the plant's leaves, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nleaf1 + mySpecies[s].Nleaf2 + mySpecies[s].Nleaf3 + mySpecies[s].Nleaf4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in the plant's stems, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStemN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nstem1 + mySpecies[s].Nstem2 + mySpecies[s].Nstem3 + mySpecies[s].Nstem4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in the plant's stolons, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nstol1 + mySpecies[s].Nstol2 + mySpecies[s].Nstol3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in the plant's roots, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRootN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nroot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in the plant's shoot, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesShootN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nshoot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average N concentration in leaves, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesLeafNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = (mySpecies[s].Ncleaf1 * mySpecies[s].dmleaf1)
                          + (mySpecies[s].Ncleaf2 * mySpecies[s].dmleaf2)
                          + (mySpecies[s].Ncleaf3 * mySpecies[s].dmleaf3)
                          + (mySpecies[s].Ncleaf4 * mySpecies[s].dmleaf4);
                if (result[s] > 0.0)
                    result[s] /= mySpecies[s].dmleaf;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average N concentration in stems, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStemNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = (mySpecies[s].Ncstem1 * mySpecies[s].dmstem1)
                          + (mySpecies[s].Ncstem2 * mySpecies[s].dmstem2)
                          + (mySpecies[s].Ncstem3 * mySpecies[s].dmstem3)
                          + (mySpecies[s].Ncstem4 * mySpecies[s].dmstem4);
                if (result[s] > 0.0)
                    result[s] /= mySpecies[s].dmstem;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average N concentration in stolons, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStolonNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                if (mySpecies[s].dmstol > 0.0)
                {
                    result[s] = (mySpecies[s].Ncstol1 * mySpecies[s].dmstol1)
                              + (mySpecies[s].Ncstol2 * mySpecies[s].dmstol2)
                              + (mySpecies[s].Ncstol3 * mySpecies[s].dmstol3);
                    if (result[s] > 0.0)
                        result[s] /= mySpecies[s].dmstol;
                }
                else
                    result[s] = 0.0;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average N concentration in roots, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesRootNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].Ncroot * mySpecies[s].dmroot;
                if (result[s] > 0.0)
                    result[s] /= mySpecies[s].dmroot;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Average N concentration in shoot, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesShootNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = MathUtility.Divide(mySpecies[s].Nshoot, mySpecies[s].dmshoot, 0.0);
            }
            return result;
        }
    }
    
    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of leaves at stage 1 (young) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage1Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmleaf1;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of leaves at stage 2 (developing) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage2Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmleaf2;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of leaves at stage 3 (mature) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage3Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmleaf3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of leaves at stage 4 (dead) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage4Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmleaf4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of stems at stage 1 (young) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage1Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmstem1;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of stems at stage 2 (developing) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage2Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmstem2;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of stems at stage 3 (mature) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage3Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmstem3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of stems at stage 4 (dead) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage4Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmstem4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of stolons at stage 1 (young) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonStage1Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmstol1;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of stolons at stage 2 (developing) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonStage2Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmstol2;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Dry matter weight of stolons at stage 3 (mature) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonStage3Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmstol3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in leaves at stage 1 (young) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage1N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nleaf1;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in leaves at stage 2 (developing) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage2N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nleaf2;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in leaves at stage 3 (mature) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage3N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nleaf3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in leaves at stage 4 (dead) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage4N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nleaf4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in stems at stage 1 (young) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage1N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nstem1;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in stems at stage 2 (developing) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage2N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nstem2;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in stems at stage 3 (mature) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage3N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nstem3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in stems at stage 4 (dead) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage4N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nstem4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in stolons at stage 1 (young) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonStage1N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nstol1;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in stolons at stage 2 (developing) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonStage2N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nstol2;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N amount in stolons at stage 3 (mature) for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonStage3N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Nstol3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N concentration of leaves at stage 1 (young) for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesLeafStage1NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ncleaf1;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N concentration of leaves at stage 2 (developing) for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesLeafStage2NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ncleaf2;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N concentration of leaves at stage 3 (mature) for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesLeafStage3NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ncleaf3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N concentration of leaves at stage 4 (dead) for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesLeafStage4NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ncleaf4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N concentration of stems at stage 1 (young) for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStemStage1NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ncstem1;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N concentration of stems at stage 2 (developing) for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStemStage2NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ncstem2;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N concentration of stems at stage 3 (mature) for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStemStage3NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ncstem3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N concentration of stems at stage 4 (dead) for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStemStage4NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ncstem4;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N concentration of stolons at stage 1 (young) for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStolonStage1NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ncstol1;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N concentration of stolons at stage 2 (developing) for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStolonStage2NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ncstol2;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("N concentration of stolons at stage 3 (mature) for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStolonStage3NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ncstol3;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Gross potential growth for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesPotGrowthGross
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Pgross / CarbonFractionDM;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Net potential growth for each species (after respiration)")]
    [Units("kgDM/ha")]
    public double[] SpeciesPotGrowthNet
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dGrowthPot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Potential growth, after water stress, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesPotGrowthW
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dGrowthW;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Actual growth for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesGrowthWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dGrowth;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Actual above-ground growth for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesHerbageGrowth
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmshoot - mySpecies[s].prevState.dmshoot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Litter amount deposited onto soil surface, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesLitterWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dLitter;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of senesced roots added to soil FOM, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesRootSenescedWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dRootSen;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of dry matter harvestable for each species (leaf+stem)")]
    [Units("kgDM/ha")]
    public double[] SpeciesHarvestableWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = Math.Max(0.0, mySpecies[s].dmleaf_green + mySpecies[s].dmstem_green - mySpecies[s].dmgreenmin)
                          + Math.Max(0.0, mySpecies[s].dmdead - mySpecies[s].dmdeadmin);
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of plant dry matter removed by harvest, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesHarvestWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmdefoliated;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Proportion in the dry matter harvested of each species")]
    [Units("%")]
    public double[] SpeciesHarvestPct
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            double myTotal = StandingPlantWt;
            for (int s = 0; s < NumSpecies; s++)
            {
                if (myTotal > 0.0)
                    result[s] = (mySpecies[s].dmstem + mySpecies[s].dmleaf) * 100 / myTotal;
            }
            return result;
        }
    }

    private double[] FractionToHarvest;
    /// <summary>An output</summary>
    [Output]
    [Description("Fraction to harvest for each species")]
    [Units("0-1")]
    public double[] SpeciesHarvestFraction
    {
        get { return FractionToHarvest; }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Rate of turnover for live DM, for each species")]
    [Units("0-1")]
    public double[] SpeciesLiveDMTurnoverRate
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].gama;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Rate of turnover for dead DM, for each species")]
    [Units("0-1")]
    public double[] SpeciesDeadDMTurnoverRate
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].gamaD;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Rate of DM turnover for stolons, for each species")]
    [Units("0-1")]
    public double[] SpeciesStolonDMTurnoverRate
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].gamaS;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Rate of DM turnover for roots, for each species")]
    [Units("0-1")]
    public double[] SpeciesRootDMTurnoverRate
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].gamaR;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N from senescing tissue potentially remobilisable, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesSenescedNRemobilisable
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].Nremob;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N remobilised from senesced material, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesSenescedNRemobilised
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].remob2NewGrowth;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of luxury N potentially remobilisable, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRemobilisableNLuxury
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].NLuxury2 + mySpecies[s].NLuxury3;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of luxury N remobilised, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLuxuryNRemobilised
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].NFastRemob2 + mySpecies[s].NFastRemob3;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of atmospheric N fixed, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesFixedN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].Nfix;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N required with luxury uptake, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRequiredNLuxury
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].NdemandLux;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N required for optimum growth, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRequiredNOptimum
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].NdemandOpt;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N demanded from soil, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesDemandN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].soilNdemand;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N in new growth, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesGrowthN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].newGrowthN;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Nitrogen concentration in new growth, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesGrowthNconc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                if (mySpecies[s].dGrowth > 0.0)
                    result[s] = mySpecies[s].newGrowthN / mySpecies[s].dGrowth;
                else
                    result[s] = 0.0;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N uptake, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesUptakeN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].soilNuptake;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N deposited as litter onto soil surface, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLitterN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].dNLitter;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of N from senesced roots added to soil FOM, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRootSenescedN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].dNrootSen;
            }
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of plant nitrogen removed by harvest, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesHarvestN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ndefoliated;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of water demand for each species")]
    [Units("mm")]
    public double[] SpeciesWaterDemand
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].WaterDemand;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Amount of water uptake for each species")]
    [Units("mm")]
    public double[] SpeciesWaterUptake
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].soilWaterUptake;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Stress factor on photosynthesis due to temperature, for each species")]
    [Units("0-1")]
    public double[] SpeciesTstress
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].lowTempStress * mySpecies[s].highTempStress;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Stress factor on photosynthesis due to high temperatures, for each species")]
    [Units("0-1")]
    public double[] SpeciesHighTstress
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].highTempStress;
            return result;
        }
    }
    /// <summary>An output</summary>
    [Output]
    [Description("Stress factor on photosynthesis due to low temperatures, for each species")]
    [Units("0-1")]
    public double[] SpeciesLowTstress
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].lowTempStress;
            return result;
        }
    }
    /// <summary>An output</summary>
    [Output]
    [Description("Growth limiting factor due to nitrogen, for each species")]
    [Units("0-1")]
    public double[] SpeciesGLFN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].glfN;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Growth limiting factor due to temperature, for each species")]
    [Units("0-1")]
    public double[] SpeciesGLFT
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            double Tmnw = (0.75 * MetData.MaxT) + (0.25 * MetData.MinT);  // weighted Tmean
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].GFTemperature(Tmnw);
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Growth limiting factor due to water deficit, for each species")]
    [Units("0-1")]
    public double[] SpeciesGLFW
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].glfWater;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Irradiance per leaf area on the top of canopy")]
    [Units("W/m^2 leaf")]
    public double[] SpeciesIrradianceTopCanopy
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].IL;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Potential C assimilation, corrected for extreme temperatures")]
    [Units("kgC/ha")]
    public double[] SpeciesPotCarbonAssimilation
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Pgross;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Loss of C via respiration")]
    [Units("kgC/ha")]
    public double[] SpeciesCarbonLossRespiration
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Resp_m + mySpecies[s].Resp_g;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Gross primary productivity")]
    [Units("kgC/ha")]
    public double GPP
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Pgross;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Net primary productivity")]
    [Units("kgC/ha")]
    public double NPP
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += (mySpecies[s].Pgross * mySpecies[s].growthEfficiency) - mySpecies[s].Resp_m;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Net above-ground primary productivity")]
    [Units("kgC/ha")]
    public double NAPP
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += ((mySpecies[s].Pgross * mySpecies[s].growthEfficiency) - mySpecies[s].Resp_m) * mySpecies[s].fShoot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Net below-ground primary productivity")]
    [Units("kgC/ha")]
    public double NBPP
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += ((mySpecies[s].Pgross * mySpecies[s].growthEfficiency) - mySpecies[s].Resp_m) * (1.0 - mySpecies[s].fShoot);
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Fraction of DM allocated to shoot")]
    [Units("0-1")]
    public double[] speciesFShoot
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].fShoot;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Fraction of shoot DM allocated to leaves")]
    [Units("0-1")]
    public double[] speciesFLeaf
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].fLeaf;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Plant height for each species")]
    [Units("mm")]
    public double[] speciesHeight
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].height;
            return result;
        }
    }

    /// <summary>An output</summary>
    [Output]
    [Description("Solar radiation intercepted by each species")]
    [Units("MJ")]
    public double[] speciesInterceptedRadn
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].interceptedRadn;
            return result;
        }
    }

    #endregion
}

////-----------------------------------------------------------------------------------------------

/// <summary>
/// Linear interpolation type
/// </summary>
public class LinearInterpolation
{
    /// <summary>
    /// The X and Y values, pairwise
    /// </summary>
    [Param]
    public string[] XYs;

    private double[] X;
    private double[] Y;

    /// <summary>
    /// Initialise the function
    /// </summary>
    [EventHandler]
    public void OnInitialised()
    {
        X = new double[XYs.Length];
        Y = new double[XYs.Length];
        for (int i = 0; i < XYs.Length; i++)
        {
            string[] XYBits = XYs[i].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (XYBits.Length != 2)
                throw new Exception("Invalid XY coordinate for function. Value: " + XYs[i]);
            X[i] = Convert.ToDouble(XYBits[0]);
            Y[i] = Convert.ToDouble(XYBits[1]);
        }
    }

    /// <summary>
    /// Interpolation
    /// </summary>
    /// <param name="dX">X value</param>
    /// <returns>Y value</returns>
    public double Value(double dX)
    {
        bool DidInterpolate = false;
        return MathUtility.LinearInterpReal(dX, X, Y, out DidInterpolate);
    }
}
