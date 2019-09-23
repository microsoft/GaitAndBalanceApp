
using System.Linq;

namespace GaitAndBalanceApp.Analysis
{
    public static class Exercises
    {
        class ExerciseData
        {
            public string Name { get; set; }
            public string Instructions { get; set; }
            public Analyzer Analyzer { get; set; }
            public string Setup { get; set; }
        };

        static SwayAnalyzer swayAnalyzer = new SwayAnalyzer { name = "Sway" };
        static GaitAnalyzer gaitAnalyzer = new GaitAnalyzer { name = "Gait" };
        static TugAnalyzer tugAnalyzer = new TugAnalyzer { name = "TUG" };
        static SwayAnalyzer swayEyesClosedAnalyzer = new SwayAnalyzer { name = "SwayEyesClosed" };
        static SwayAnalyzer swayFoamAnalyzer = new SwayAnalyzer { name = "SwayFoam" };
        static SwayAnalyzer swayFoamEyesClosedAnalyzer = new SwayAnalyzer { name = "SwayFoamEyesClosed" };
        static SegmentedGaitAnalyzer segmentedGaitAnalyzer = new SegmentedGaitAnalyzer { name = "SegmentedGait" };
        static ChairStandAnalyzer chairStandAnalyzer = new ChairStandAnalyzer { name = "ChairStand" };
        static FourStepSquareAnalyzer fourStepSquareAnalyzer = new FourStepSquareAnalyzer { name = "FourStepSquare" };
        static ReachAnalyzer reachAnalyzer = new ReachAnalyzer { name = "Reach" };

        static ExerciseData[] availableExercises = new ExerciseData[] {
            new ExerciseData(){Name = "Sway", Instructions = "Please stand with feet touching each other and arms crossed high.", Analyzer = swayAnalyzer, Setup = "The subject should be standing 2 meters from the sensor, centered and parallel to the sensor"},
            new ExerciseData(){Name = "Sway Eyes Closed", Instructions = "Please stand with feet touching each other and arms crossed high. Close your eyes during this exercise.", Analyzer = swayEyesClosedAnalyzer, Setup = "The subject should be standing 2 meters from the sensor, centered and parallel to the sensor"},
            new ExerciseData(){Name = "Sway on Foam", Instructions = "Please stand on the foam with feet touching each other and arms crossed high.", Analyzer = swayFoamAnalyzer, Setup = "Place a foam 2 meters away from the sensor, centered. The subject should be standing on the foam, parallel to the Kinect sensor"},
            new ExerciseData(){Name = "Sway on Foam Eyes Closed", Instructions = "Please stand on the foam with feet touching each other and arms crossed high. Close your eyes during this exercise.", Analyzer = swayFoamEyesClosedAnalyzer, Setup = "Place a foam 2 meters away from the sensor, centered. The subject should be standing on the foam, parallel to the Kinect sensor"},
            new ExerciseData(){Name = "TUG", Instructions = "When instructed, stand up, walk around the mark, return to the chair and sit.", Analyzer = tugAnalyzer, Setup = "The subject should be sitting, facing the sensor, 5 meters away from the sensor. A mark or a cone should be places 2 meters away from the sensor."},
            new ExerciseData(){Name = "Gait", Instructions = "Cover as much ground as possible over 2 minutes. Walk continuously if possible, but do not be concerned if you need to slow down or stop to rest. The goal is to feel at the end of the test that more ground could not have been covered in the 2 minutes.", Analyzer = gaitAnalyzer, Setup = "Place a cone or another marker at a distance of 2 meters and 6 meters from the sensor. The subject should start next to the closer mark and walk around the markers."},
            new ExerciseData(){Name = "Segmented Gait", Instructions = "Instructions should be defined in the config file.", Analyzer =  segmentedGaitAnalyzer, Setup = "Setup should be defined in the config file."},
            new ExerciseData(){Name = "Tandem", Instructions = "Please stand with one foot in front of the other.", Analyzer = swayAnalyzer, Setup = "The subject should be standing 2 meters from the sensor, centered and parallel to the sensor"},
            new ExerciseData(){Name = "SemiTandem", Instructions = "Please stand with one foot slightly in front of the other, so the big toe of the back foot touchesthe instep of the front foot.", Analyzer = swayAnalyzer, Setup = "The subject should be standing 2 meters from the sensor, centered and parallel to the sensor"},
            new ExerciseData(){Name = "Chair Stand", Instructions = "Sit in the middle of the chair. Place each hand on the opposite shoulder crossed at the wrists. Place your feet flat on the floor. Keep your back straight and keep your arms against your chest. On “Go”, rise to a full standing position and then sit back down again.", Analyzer =  chairStandAnalyzer, Setup = "The chair should be placed 2 meters from the sensor, centered and parallel to the sensor"},
            new ExerciseData(){Name = "Reach", Instructions = "Please stand and try to reach your hand as long as you can", Analyzer = reachAnalyzer, Setup = "The subject should be standing 2 meters from the sensor, centered and parallel to the sensor"},
            new ExerciseData(){Name = "Four Step Square", Instructions = "Please step as fast as possible into eadc square with both feet in the following sequence: Square 1,2,3,4,1,4,3,2,1 .", Analyzer =  fourStepSquareAnalyzer, Setup = "The vertical line should be placed 2 meters away from the sensor, centered and parallel to the sensor, the horizontal line should be placed centered and parallel to the sensor. subject should be standing behind the line in square 1"},

        };

        public static Analyzer GetAnalyzer(string exercise)
        {
            if (availableExercises == null) return null;
            foreach (var e in availableExercises)
                if (e.Name == exercise)
                    return e.Analyzer;
            return null;
        }

        static ExerciseData GetExerciseByName(string exercise)
        {
            if (availableExercises == null) return null;
            foreach (var e in availableExercises)
                if (e.Name == exercise)
                    return e;
            return null;
        }

        public static string GetDescription(string exercise)
        {
            var e = GetExerciseByName(exercise);
            if (e == null) return null;
            var t = Metrics.getMetaData(e.Analyzer.name, "Instructions");
            if (t != null) return t.description;
            return e.Instructions;
        }

        public static string GetSetup(string exercise)
        {
            var e = GetExerciseByName(exercise);
            if (e == null) return null;
            var t = Metrics.getMetaData(e.Analyzer.name, "Setup");
            if (t != null) return t.description;
            return e.Setup;
        }

        public static string[] GetExercises()
        {
            if (availableExercises == null) return null;
            return availableExercises.Select(e => e.Name).ToArray();
        }


    }
}
