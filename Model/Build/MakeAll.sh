#! /bin/sh

cd ../..
export APSIM=`pwd`
cd $APSIM/Model/Build

ulimit -s unlimited
rm -f $APSIM/Model/Build/Build.out

if [ "$1" != "clean" ]; then tclsh VersionStamper.tcl; fi

# -------------------------------------------------------------
# This script compiles the APSIM engine and modules
# -------------------------------------------------------------

# -------------------------------------------------------------
# Need to process the datatypes.interface file and auto-
# generate the datatypes.cpp, .h etc.
# -------------------------------------------------------------
./MakeProject.sh CSGeneral $1
./MakeProject.sh CMPServices $1
./MakeProject.sh ApsimFile $1
./MakeProject.sh DataTypes $1

# -------------------------------------------------------------
# Need to compile all infrastructure projects before the main
# bulk of projects
# -------------------------------------------------------------
./MakeProject.sh General $1
./MakeProject.sh ApsimShared $1
./MakeProject.sh Protocol $1
./MakeProject.sh ComponentInterface $1
./MakeProject.sh ComponentInterface2 $1
./MakeProject.sh CSDotNetComponentInterface $1
./MakeProject.sh FortranInfrastructure $1
./MakeProject.sh FortranComponentInterface $1
./MakeProject.sh FortranComponentInterface2 $1
./MakeProject.sh ProtocolManager $1
./MakeProject.sh CropTemplate $1
./MakeProject.sh DotNetProxies $1
./MakeProject.sh ConToSim         $1
./MakeProject.sh ConToApsim       $1
./MakeProject.sh ApsimToSim       $1
./MakeProject.sh Apsim $1

# -------------------------------------------------------------
# Now we can compile all the rest of the APSIM projects.
# -------------------------------------------------------------
./MakeProject.sh Accum            $1
./MakeProject.sh AgPasture        $1
./MakeProject.sh Canopy           $1
./MakeProject.sh Clock            $1
./MakeProject.sh CropMod          $1
./MakeProject.sh Eo               $1
./MakeProject.sh Erosion          $1
./MakeProject.sh Fertiliser       $1
./MakeProject.sh Grasp            $1
./MakeProject.sh Graz             $1
./MakeProject.sh Growth           $1
./MakeProject.sh Input            $1
./MakeProject.sh Irrigation       $1
./MakeProject.sh Log              $1
./MakeProject.sh Maize            $1
./MakeProject.sh MaizeZ           $1
./MakeProject.sh Manager          $1
./MakeProject.sh Manager2         $1
./MakeProject.sh Map              $1
./MakeProject.sh MicroClimate     $1
./MakeProject.sh MicroMet         $1
./MakeProject.sh Millet           $1
./MakeProject.sh Operations       $1
./MakeProject.sh Oryza            $1
./MakeProject.sh Ozcot            $1
./MakeProject.sh Parasite         $1
./MakeProject.sh Pasture          $1
./MakeProject.sh PatchInput       $1
./MakeProject.sh Plant            $1
./MakeProject.sh Plant2           $1
./MakeProject.sh Pond             $1
./MakeProject.sh ProtocolManager  $1
./MakeProject.sh Report           $1
./MakeProject.sh Root             $1
./MakeProject.sh SiloInput        $1
./MakeProject.sh SOI              $1
./MakeProject.sh SoilErosion      $1
./MakeProject.sh SoilN            $1
./MakeProject.sh SoilNitrogen     $1
./MakeProject.sh SoilP            $1
./MakeProject.sh SoilTemp         $1
./MakeProject.sh SoilTemp2        $1
./MakeProject.sh SoilWat          $1
./MakeProject.sh SoilWater        $1
./MakeProject.sh Solute           $1
./MakeProject.sh Sorghum          $1
./MakeProject.sh Stock            $1
./MakeProject.sh Sugar            $1
./MakeProject.sh Supplement       $1
./MakeProject.sh Surface          $1
./MakeProject.sh SurfaceOM        $1
./MakeProject.sh SurfaceOrganicMatter        $1
./MakeProject.sh SurfaceTemp      $1
./MakeProject.sh SWIM2            $1
./MakeProject.sh SWIM3            $1
./MakeProject.sh SysBal           $1
./MakeProject.sh SysBal2          $1
./MakeProject.sh TclLink          $1
./MakeProject.sh Tracker          $1
./MakeProject.sh Tree             $1
./MakeProject.sh UrinePatch       $1
#./MakeProject.sh VenLink          $1
./MakeProject.sh WaterSupply      $1
./MakeProject.sh YieldProphet     $1
./MakeProject.sh DDRules          $1
./MakeProject.sh FarmSimGraze     $1
./MakeProject.sh ApsimX           $1
./MakeProject.sh JobScheduler/CreatePatch      $1

