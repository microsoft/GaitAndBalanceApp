
using System.Linq;

namespace GaitAndBalanceApp.Analysis
{
    public static class Exercises
    {
        class ExerciseData
        {
            public string name { get; set; }
            public string instructions { get; set; }
            public Analyzer analyzer { get; set; }
            public string setup { get; set; }
        };

        static SwayAnalyzer swayAnalyzer = new SwayAnalyzer { name = "Sway" };
        static GaitAnalyzer gaitAnalyzer = new GaitAnalyzer { name = "Gait" };
        static TugAnalyzer tugAnalyzer = new TugAnalyzer { name = "TUG" };
        static SwayAnalyzer swayEyesClosedAnalyzer = new SwayAnalyzer { name = "SwayEyesClosed" };
        static SwayAnalyzer swayFoamAnalyzer = new SwayAnalyzer { name = "SwayFoam" };
        static SwayAnalyzer swayFoamEyesClosedAnalyzer = new SwayAnalyzer { name = "SwayFoamEyesClosed" };
        static SegmentedGaitAnalyzer segmentedGaitAnalyzer = new SegmentedGaitAnalyzer { name = "SegmentedGait" };

        static ExerciseData[] availableExercises = new ExerciseData[] {
            new ExerciseData(){name = "Sway", instructions = "Please stand with feet touching each other and arms crossed high.", analyzer = swayAnalyzer, setup = "The subject should be standing 2 meters from the sensor, centered and parallel to the sensor"},
            new ExerciseData(){name = "Sway Eyes Closed", instructions = "Please stand with feet touching each other and arms crossed high. Close your eyes during this exercise.", analyzer = swayEyesClosedAnalyzer, setup = "The subject should be standing 2 meters from the sensor, centered and parallel to the sensor"},
            new ExerciseData(){name = "Sway on Foam", instructions = "Please stand on the foam with feet touching each other and arms crossed high.", analyzer = swayFoamAnalyzer, setup = "Place a foam 2 meters away from the sensor, centered. The subject should be standing on the foam, parallel to the Kinect sensor"},
            new ExerciseData(){name = "Sway on Foam Eyes Closed", instructions = "Please stand on the foam with feet touching each other and arms crossed high. Close your eyes during this exercise.", analyzer = swayFoamEyesClosedAnalyzer, setup = "Place a foam 2 meters away from the sensor, centered. The subject should be standing on the foam, parallel to the Kinect sensor"},
            new ExerciseData(){name = "TUG", instructions = "When instructed, stand up, walk around the mark, return to the chair and sit.", analyzer = tugAnalyzer, setup = "The subject should be sitting, facing the sensor, 5 meters away from the sensor. A mark or a cone should be places 2 meters away from the sensor."},
            new ExerciseData(){name = "Gait", instructions = "Cover as much ground as possible over 2 minutes. Walk continuously if possible, but do not be concerned if you need to slow down or stop to rest. The goal is to feel at the end of the test that more ground could not have been covered in the 2 minutes.", analyzer = gaitAnalyzer, setup = "Place a cone or another marker at a distance of 2 meters and 6 meters from the sensor. The subject should start next to the closer mark and walk around the markers."},
            new ExerciseData(){name = "Segmented Gait", instructions = "Instructions should be defined in the config file.", analyzer =  segmentedGaitAnalyzer, setup = "Setup should be defined in the config file."},
        };

        public static Analyzer getAnalyzer(string exercise)
        {
            if (availableExercises == null) return null;
            foreach (var e in availableExercises)
                if (e.name == exercise)
                    return e.analyzer;
            return null;
        }

        static ExerciseData getExerciseByName(string exercise)
        {
            if (availableExercises == null) return null;
            foreach (var e in availableExercises)
                if (e.name == exercise)
                    return e;
            return null;
        }

        public static string getDescription(string exercise)
        {
            var e = getExerciseByName(exercise);
            if (e == null) return null;
            var t = Metrics.getMetaData(e.analyzer.name, "Instructions");
            if (t != null) return t.description;
            return e.instructions;
        }

        public static string getSetup(string exercise)
        {
            var e = getExerciseByName(exercise);
            if (e == null) return null;
            var t = Metrics.getMetaData(e.analyzer.name, "Setup");
            if (t != null) return t.description;
            return e.setup;
        }

        public static string[] getExercises()
        {
            if (availableExercises == null) return null;
            return availableExercises.Select(e => e.name).ToArray();
        }


    }
}
