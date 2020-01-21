using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MediaSyncTestsConsoleApp1
{
    public class Instruction
    {
        public string fileName { get; set; }
        public string start { get; set; }
        public string end { get; set; }

    }

    public class clipTime
    {
        public int hour { get; set; }
        public int min { get; set; }
        public int sec { get; set; }
        public int dec { get; set; }
    }


    public class VidsStream
    {
        public int index { get; set; }
        public string codec_type { get; set; }
        public double duration { get; set; }
        public int height { get; set; }
        public int width { get; set; }
    }

    public class Vids
    {
        public IEnumerable<VidsStream> streams { get; set; }
    }

    class Program
    {
        const string ffmpegLocation = @"C:\Program Files (x86)\ffmpeg\bin";
        const string ffmpeg = ffmpegLocation + @"\ffmpeg.exe";
        const string ffprobe = ffmpegLocation + @"\ffprobe.exe";


        static string Uniform(string inputFile, string outputFile)
        {
            var a2 = $@"-i ""{inputFile}"" -vf ""scale=w=1280:h=720:force_original_aspect_ratio=1,pad=1280:720:(ow-iw)/2:(oh-ih)/2,setsar = sar = 1 / 1,setdar = dar = 16 / 9"" -y {outputFile}";

             var psi = new ProcessStartInfo(ffmpeg, a2)
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            };

            var p = new Process { StartInfo = psi };
            p.Start();

            while (!p.HasExited)
            {
                System.Threading.Thread.Sleep(100);
            }
            return outputFile;
        }

        static string Combine(List<string> inputs, string outputFile)
        {
            outputFile = outputFile.Replace(" ", string.Empty);
            var endStuff = $@"-vsync 2 -filter_complex ""[0:v] [0:a] [1:v] [1:a] concat=n={inputs.Count().ToString()}:v = 1:a = 1[v][a]"" -map ""[v]"" -map ""[a]"" -y {outputFile}.mp4 ";
            var startStuff = "";
            foreach (var i in inputs)
            {
                startStuff += "-i " + i + " ";
            }
            var a2 = startStuff + endStuff;

            var psi = new ProcessStartInfo(ffmpeg, a2)
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            };

            var p = new Process { StartInfo = psi };
            p.Start();

            while (!p.HasExited)
            {
                System.Threading.Thread.Sleep(100);
            }

            return outputFile;
        }

        static string Combine(string firstFile, string secondFile, string outputFile)
        {

            //JZ get full path for files
            //add photo for 5 seconds

            // https://stackoverflow.com/questions/7333232/how-to-concatenate-two-mp4-files-using-ffmpeg
            var a2 = $@" -i ""{firstFile}"" -i ""{secondFile}"" -vsync 1 -filter_complex ""[0:v] [0:a] [1:v] [1:a] concat=n=2:v=1:a=1 [v] [a]"" -map ""[v]"" -map ""[a]"" ""{outputFile}""";
            var psi = new ProcessStartInfo(ffmpeg, a2)
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            };

            var p = new Process { StartInfo = psi };
            p.Start();

            while (!p.HasExited)
            {
                System.Threading.Thread.Sleep(100);
            }

            return outputFile;
        }

        static string Split(string start, string inputFile, string end, string outputNum )
        {
            var outputFile = outputNum + ".mp4";
            //picturename/cameraname/number

            //https://stackoverflow.com/questions/45004159/ffmpeg-ss-and-t-for-cutting-mp3

            // -y command automatically overrides files
            var a2 = $@"-i ""{inputFile}"" -ss {start} -to {end} -c:v libx264 -y ""{outputFile}""";
            var psi = new ProcessStartInfo(ffmpeg, a2)
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            };

            var p = new Process { StartInfo = psi };
            p.Start();

            while (!p.HasExited)
            {
                System.Threading.Thread.Sleep(100);
            }

            return outputFile;
        }


        static public Vids GetInfo(string inputFile)
        {
            //ffprobe -v quiet "CAM1.mp4" -print_format json -show_entries stream=codec_type,duration,height,width"
            string a = $@" -v quiet ""{inputFile}"" -print_format json -show_entries stream=index,codec_type,duration,height,width";
            var psi = new ProcessStartInfo(ffprobe, a)
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var p = new Process { StartInfo = psi };
            p.Start();

            while (!p.HasExited)
            {
                System.Threading.Thread.Sleep(100);
            }

            var serializer = new JsonSerializer();

            using (var jsonTextReader = new JsonTextReader(p.StandardOutput))
            {
                var o = serializer.Deserialize<Vids>(jsonTextReader);
                return o;
            }
        }

        static void ProcessFolder(string folder)
        {
            // output each trigger and which footage files that contain it
            // output lists of footage folders in chronological order
            // output list of everything in chronological order

            var lines = new string[10];
            var instructions = new List<Instruction>();
            var p = Path.GetFullPath(folder);


            foreach (var file in Directory.EnumerateFiles(folder))
            {
                Console.WriteLine(file);
                var f = new FileInfo(file);
                if (f.Name == "test.txt")
                {
                    var contents = System.IO.File.ReadAllText(f.FullName);
                    lines = contents.Split(';');
                }

            }

            foreach (string line in lines)
            {
                if (line.Length > 4)
                {
                    var l = line.Split(' ');
                    string fp = Regex.Match(l[0], @".*\.mp4").Value;
            

                    DateTime s = DateTime.Parse(l[1]);
                    DateTime e = DateTime.Parse(l[2]);
                    var f = p + '\\' + fp;

                    instructions.Add(new Instruction
                    {
                        fileName = f,
                        start = s.TimeOfDay.ToString(),
                        end = e.TimeOfDay.ToString()
                    });
                }

            }

            foreach (var i in instructions)
            {
                i.start = EstablishStart(i.fileName, i.start);
                i.end = EstablishEnd(i.fileName, i.end);
            }

            int on = 0;
            int un = 100;
            var clips = new List<string>();
            foreach (var i in instructions)
            {
                var info = GetInfo(i.fileName);

                un++;

                //  clips.Add(Split(i.start.ToString(), unif, i.duration.ToString(), un.ToString()));
                var spl = Split(i.start, i.fileName, i.end, un.ToString());
                var uni = Uniform(spl, "U" + un.ToString() + ".mp4");
                clips.Add(uni);


            }

            Combine(clips, p + "\\output");

        }



        static void Main(string[] args)
        {

            string sourceFolder = args[0];

            ProcessFolder(sourceFolder);

        }

        static string EstablishStart(string file, string currentStart)
        {
            bool good = false;
            string edited = currentStart;
            while (!good)
            {
                CheckStart(edited, file);
                Console.WriteLine("Move? in ms");
                var msMove = Int32.Parse(Console.ReadLine()) * 10000;

                if (msMove == 0)
                {
                    good = true;
                }
                else
                {
                    edited = (DateTime.Parse(edited).TimeOfDay + new TimeSpan(msMove)).ToString();
                }
            }

            return edited;
        }

        static void CheckStart(string start, string inputFile)
        {
            var outputFile = "C:\\Users\\jacob\\xrepos\\debug\\checked.mp4";

            var a2 = $@"-i ""{inputFile}"" -ss {start} -t 3 -c:v libx264 -y ""{outputFile}""";
            var psi = new ProcessStartInfo(ffmpeg, a2)
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            };

            var p = new Process { StartInfo = psi };
            p.Start();

            while (!p.HasExited)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        static string EstablishEnd(string file, string currentEnd)
        {
            bool good = false;
            string edited = currentEnd;
            while (!good)
            {
                var start = (DateTime.Parse(edited).TimeOfDay - new TimeSpan(0, 0, 3)).ToString();
                CheckEnd(start, file);
                Console.WriteLine("Move? in ms");
                var msMove = Int32.Parse(Console.ReadLine()) * 10000;

                if (msMove == 0)
                {
                    good = true;
                }
                else
                {
                    edited = (DateTime.Parse(edited).TimeOfDay + new TimeSpan(msMove)).ToString();
                }
            }

            return edited;
        }

        static void CheckEnd(string start, string inputFile)
        {
            var outputFile = "C:\\Users\\jacob\\xrepos\\debug\\checked.mp4";

            var a2 = $@"-i ""{inputFile}"" -ss {start} -t 3 -c:v libx264 -y ""{outputFile}""";
            var psi = new ProcessStartInfo(ffmpeg, a2)
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            };

            var p = new Process { StartInfo = psi };
            p.Start();

            while (!p.HasExited)
            {
                System.Threading.Thread.Sleep(100);
            }
        }
    }




}



