﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using TwilightShards.genLibrary;
using TwilightShards.GURPSUtil;

namespace GenerateGURPSPlanet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> worldSize;
        public ObservableCollection<string> planetType;
        public ObservableCollection<string> parentType;
        protected Dice ourDice;

        public MainWindow()
        {
            InitializeComponent();
            worldSize = new ObservableCollection<string>();
            worldSize.Add("Tiny");
            worldSize.Add("Small");
            worldSize.Add("Standard");
            worldSize.Add("Large");

            parentType = new ObservableCollection<string>();
            parentType.Add("Star");
            parentType.Add("Terrestial Planet");
            parentType.Add("Gas Giant Planet");

            planetType = new ObservableCollection<string>();
            planetType.Add("Moon");
            planetType.Add("Terrestial");
            planetType.Add("Gas Giant");

            cmbParenType.ItemsSource = parentType;
            cmbWorldSize.ItemsSource = worldSize;
            cmbWorldType.ItemsSource = planetType;
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            double star1Lumin, star2Lumin, star3Lumin, star4Lumin, star5Lumin, star1Distance, star2Distance, star3Distance, star4Distance, 
                star5Distance, parentMass, primaryMass, systemAge;

            Planet currPlanet = new Planet();

            if (!(Double.TryParse(txtStar1Luminosity.Text, out star1Lumin)))
                return;
            if (!(Double.TryParse(txtStar1OrbitalDistance.Text, out star1Distance)))
                return;
            if (!(Double.TryParse(txtStar2Luminosity.Text, out star2Lumin)))
                return;
            if (!(Double.TryParse(txtStar2OrbitalDistance.Text, out star2Distance)))
                return;
            if (!(Double.TryParse(txtStar3Luminosity.Text, out star3Lumin)))
                return;
            if (!(Double.TryParse(txtStar3OrbitalDistance.Text, out star3Distance)))
                return;
            if (!(Double.TryParse(txtStar4Luminosity.Text, out star4Lumin)))
                return;
            if (!(Double.TryParse(txtStar4OrbitalDistance.Text, out star4Distance)))
                return;
            if (!(Double.TryParse(txtStar5Luminosity.Text, out star5Lumin)))
                return;
            if (!(Double.TryParse(txtStar5OrbitalDistance.Text, out star5Distance)))
                return;
            if (!(Double.TryParse(txtParentMass.Text, out parentMass)))
                return;
            if (!(Double.TryParse(txtPrimaryMass.Text, out primaryMass)))
                return;
            if (!(Double.TryParse(txtSystemAge.Text, out systemAge)))
                return;

            //get values from interface
            currPlanet.worldSize = StarReference.convertWorldSize(cmbWorldSize.SelectedItem.ToString());
            currPlanet.planetType = StarReference.convertPlanetType(cmbWorldType.SelectedItem.ToString());
            string selectedParentType = cmbParenType.SelectedItem.ToString();
            
            bool genMoons = false;
            if (chkGeneateMoons.IsChecked == true)
                genMoons = true;

            //populate stars.
            currPlanet.parentStars.Add(new StarRecord(0, true, star1Distance));
            if (star2Distance != 0)
                currPlanet.parentStars.Add(new StarRecord(1, false, star2Distance));
            if (star3Distance != 0)
                currPlanet.parentStars.Add(new StarRecord(2, false, star3Distance));
            if (star4Distance != 0)
                currPlanet.parentStars.Add(new StarRecord(3, false, star4Distance));
            if (star5Distance != 0)
                currPlanet.parentStars.Add(new StarRecord(4, false, star5Distance));

            //We need to roll moons. 
            if (currPlanet.planetType == PlanetType.TerrestialPlanet && genMoons)
            {
                currPlanet.majorMoons = StarReference.getTerrestialMajorMoons(ourDice, currPlanet.worldSize, star1Distance);
                if (currPlanet.majorMoons == 0)
                    currPlanet.moonlets = StarReference.getTerrestialMoonlets(ourDice, currPlanet.worldSize, star1Distance);
                else
                    currPlanet.moonlets = 0;
            }

            if (currPlanet.planetType == PlanetType.GasGiantPlanet && genMoons) 
            {
                currPlanet.ringMoons = StarReference.getGasGiantRingMoons(ourDice, star1Distance);
                currPlanet.majorMoons = StarReference.getGasGiantMajorMoons(ourDice, star1Distance);
                currPlanet.capturedMoons = StarReference.getGasGiantCapturedMoons(ourDice, star1Distance);
            }

            //calc blackbody.
            double[] blackbody = new double[5];
            if (star1Distance != 0)
                blackbody[0] = StarReference.getBlackBodyTemp(star1Lumin, star1Distance);
            if (star2Distance != 0)
                blackbody[1] = StarReference.getBlackBodyTemp(star2Lumin, star2Distance);
            if (star3Distance != 0)
                blackbody[2] = StarReference.getBlackBodyTemp(star4Lumin, star3Distance);
            if (star4Distance != 0)
                blackbody[3] = StarReference.getBlackBodyTemp(star4Lumin, star4Distance);
            if (star5Distance != 0)
                blackbody[4] = StarReference.getBlackBodyTemp(star5Lumin, star5Distance);

             currPlanet.blackbodyTemp = StarReference.getBlackbodyTempTotal(blackbody);
            

            //now that we have the blackbody, let's get the world type.
            if (currPlanet.planetType == PlanetType.TerrestialPlanet || currPlanet.planetType == PlanetType.Moon)
                currPlanet.biomeType = StarReference.getWorldType(ourDice, currPlanet.blackbodyTemp, currPlanet.worldSize, selectedParentType, primaryMass, systemAge);
            else if (currPlanet.planetType == PlanetType.GasGiantPlanet) 
                currPlanet.biomeType = WorldType.GasGiant;
            else
                currPlanet.biomeType = WorldType.None;

            //WorldType determined. Now to the next step: Atmosphere.
            currPlanet.atmoMass = 0; // set default mass.
            if (currPlanet.planetType == PlanetType.TerrestialPlanet || currPlanet.planetType == PlanetType.Moon)
            {
                
                if (currPlanet.worldSize == WorldSize.Small && currPlanet.biomeType == WorldType.Ice)
                {
                    currPlanet.atmoMass = StarReference.rollAtmosphere(ourDice);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.Suffocating);
                    if (ourDice.rollUnderGurps(15))
                        currPlanet.AddAtmosphericCondition(AtmosphericConditions.MildlyToxic);
                    else
                        currPlanet.AddAtmosphericCondition(AtmosphericConditions.HighlyToxic);

                    //set composition
                    currPlanet.atmoComposition = "Primarily Nitrogen and Methane, with components of Argon, and some trace ammonia and carbon dioxide.";
                }

                if (currPlanet.worldSize == WorldSize.Standard && currPlanet.biomeType == WorldType.Ice)
                {
                    currPlanet.atmoMass = StarReference.rollAtmosphere(ourDice);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.Suffocating);
                    if (!(ourDice.rollUnderGurps(12)))
                        currPlanet.AddAtmosphericCondition(AtmosphericConditions.MildlyToxic);

                    //set composition
                    currPlanet.atmoComposition = "Primarily Carbon Dioxide, Nitrogen, with some Methane or Sulfur Dioxide";
                }

                if (currPlanet.worldSize == WorldSize.Large  && currPlanet.biomeType == WorldType.Ice)
                {
                    currPlanet.atmoMass = StarReference.rollAtmosphere(ourDice);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.Suffocating);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.HighlyToxic);

                    //set composition
                    currPlanet.atmoComposition = "Primarily Helium, Nitrogen, with some Sulfur Dioxide, Chlorine and Flourine";
                }

                if (currPlanet.worldSize == WorldSize.Standard && currPlanet.biomeType == WorldType.Ammonia)
                {
                    currPlanet.atmoMass = StarReference.rollAtmosphere(ourDice);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.Suffocating);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.LethallyToxic);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.Corrosive);

                    //set composition
                    currPlanet.atmoComposition = "Primarily Nitrogen with a large amount of ammonia and methane.";
                }

                if (currPlanet.worldSize == WorldSize.Large && currPlanet.biomeType == WorldType.Ammonia)
                {
                    currPlanet.atmoMass = StarReference.rollAtmosphere(ourDice);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.Suffocating);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.LethallyToxic);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.Corrosive);

                    //set composition
                    currPlanet.atmoComposition = "Primarily Nitrogen with a large amount of ammonia and methane.";
                }

                if (currPlanet.worldSize == WorldSize.Standard && currPlanet.biomeType == WorldType.Ocean)
                {
                    currPlanet.atmoMass = StarReference.rollAtmosphere(ourDice);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.Suffocating);
                    if (!(ourDice.rollUnderGurps(12)))
                        currPlanet.AddAtmosphericCondition(AtmosphericConditions.Corrosive);
                    
                    //set composition
                    currPlanet.atmoComposition = "Primarily Carbon Dioxide and Nitrogen.";
                }

                if (currPlanet.worldSize == WorldSize.Large && currPlanet.biomeType == WorldType.Ocean)
                {
                    currPlanet.atmoMass = StarReference.rollAtmosphere(ourDice);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.Suffocating);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.HighlyToxic);

                    //set composition
                    currPlanet.atmoComposition = "A mixture of Helium and Nitrogen.";
                }

                if (currPlanet.worldSize == WorldSize.Standard && currPlanet.biomeType == WorldType.Garden)
                {
                    currPlanet.atmoMass = StarReference.rollAtmosphere(ourDice);

                    if (!(ourDice.rollUnderGurps(11)))
                        StarReference.addMarginalConditions(currPlanet, ourDice);
                    
                    currPlanet.atmoComposition = "Primarily Nitrogen and Oxygen. Some Argon and other trace gasses.";
                }
                if (currPlanet.worldSize == WorldSize.Large && currPlanet.biomeType == WorldType.Garden)
                {
                    currPlanet.atmoMass = StarReference.rollAtmosphere(ourDice);

                    if (!(ourDice.rollUnderGurps(11)))
                        StarReference.addMarginalConditions(currPlanet, ourDice);

                    currPlanet.atmoComposition = "Primarily Nitrogen and Oxygen as well as many inert gasses (neon, argon)";
                }
                if ((currPlanet.worldSize == WorldSize.Large || currPlanet.worldSize == WorldSize.Standard) && currPlanet.biomeType == WorldType.Greenhouse)
                {
                    currPlanet.atmoMass = StarReference.rollAtmosphere(ourDice);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.Suffocating);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.LethallyToxic);
                    currPlanet.AddAtmosphericCondition(AtmosphericConditions.Corrosive);
                    //this is either dry or wet.
                    if (ourDice.coinFlip() == true)
                    {
                        currPlanet.biomeType = WorldType.DryGreenhouse;
                        currPlanet.atmoComposition = "Primarily Carbon Dioxide and some Sulfur Dioxide.";
                    }
                    else
                    {
                        currPlanet.atmoComposition = "Carbon Dioxide, Nitorgen, and large amounts of Water Vapor, and traces of free oxygen.";
                        currPlanet.biomeType = WorldType.WetGreenhouse;
                    }
                }
            }

            //hydrographic coverage
            currPlanet.hydroCoverage = 0;
            if ((currPlanet.worldSize == WorldSize.Small) && (currPlanet.biomeType == WorldType.Ice))
            {
                currPlanet.volatileType = HydroCoverageType.Hydrocarbons;
                currPlanet.hydroCoverage = 0;
            }

            //and now we format the output! :D :D :D 
        }
    }
}
