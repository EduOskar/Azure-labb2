using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Drawing;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace Azure_labb2
{
    class Program
    {

        private static ComputerVisionClient cvClient;
        static async Task Main(string[] args)
        {
            string quit;
            try
            {

                string userResponse = null;

                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string cogSvcEndpoint = configuration["CognitiveServicesEndpoint"];
                string cogSvcKey = configuration["CognitiveServiceKey"];

                string imageFile = null;

                if (args.Length > 0)
                {
                    imageFile = args[0];
                }


                // Authenticate Computer Vision client
                // Authenticate Computer Vision client
                ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(cogSvcKey);
                cvClient = new ComputerVisionClient(credentials)
                {
                    Endpoint = cogSvcEndpoint
                };

                do
                {
                    Console.WriteLine("Enter an imageURl");
                    //Console.WriteLine("Options for imagefiles: \ncat.jpg\nmarine.jpg\nzealot.jpg\nzergling.jpg");
                    imageFile = Console.ReadLine();
                    // Analyze image
                    await AnalyzeImage(imageFile);

                    // Get thumbnail
                    await GetThumbnail(imageFile);
                    await Console.Out.WriteLineAsync("write quit to end the process");
                    quit = Console.ReadLine();

                } while (quit != "quit".ToLower());
              


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task AnalyzeImage(string imageFile)
        {
            Console.WriteLine($"Analyzing {imageFile}");

            // Specify features to be retrieved
            List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
            {
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Categories,
                VisualFeatureTypes.Brands,
                VisualFeatureTypes.Objects,
                VisualFeatureTypes.Adult
            };

            ImageAnalysis analysis = await cvClient.AnalyzeImageAsync(imageFile, features);

            // get image captions
            foreach (var caption in analysis.Description.Captions)
            {
                Console.WriteLine($"Description: {caption.Text} (confidence: {caption.Confidence.ToString("P")})");
            }

            // Get image tags
            if (analysis.Tags.Count > 0)
            {
                Console.WriteLine("Tags:");
                foreach (var tag in analysis.Tags)
                {
                    Console.WriteLine($" -{tag.Name} (confidence: {tag.Confidence.ToString("P")})");
                }
            }


            // Get image categories
            List<LandmarksModel> landmarks = new List<LandmarksModel> { };
            Console.WriteLine("Categories:");
            foreach (var category in analysis.Categories)
            {
                // Print the category
                Console.WriteLine($" -{category.Name} (confidence: {category.Score.ToString("P")})");

                // Get landmarks in this category
                if (category.Detail?.Landmarks != null)
                {
                    foreach (LandmarksModel landmark in category.Detail.Landmarks)
                    {
                        if (!landmarks.Any(item => item.Name == landmark.Name))
                        {
                            landmarks.Add(landmark);
                        }
                    }
                }
            }

            // If there were landmarks, list them
            if (landmarks.Count > 0)
            {
                Console.WriteLine("Landmarks:");
                foreach (LandmarksModel landmark in landmarks)
                {
                    Console.WriteLine($" -{landmark.Name} (confidence: {landmark.Confidence.ToString("P")})");
                }
            }



            // Get brands in the image
            if (analysis.Brands.Count > 0)
            {
                Console.WriteLine("Brands:");
                foreach (var brand in analysis.Brands)
                {
                    Console.WriteLine($" -{brand.Name} (confidence: {brand.Confidence.ToString("P")})");
                }
            }


            // Get objects in the image
            if (analysis.Objects.Count > 0)
            {
                Console.WriteLine("Objects in image:");

                foreach (var obj in analysis.Objects)
                {
                    Console.WriteLine($"{obj.ObjectProperty} with confidence {obj.Confidence} at location {obj.Rectangle}, " +
                                      $"{obj.Rectangle.X + obj.Rectangle.W}, {obj.Rectangle.Y}, {obj.Rectangle.Y + obj.Rectangle.H}");
                }
                // Prepare image for drawing

                WebClient wc = new WebClient();
                byte[] bytes = wc.DownloadData(imageFile);
                MemoryStream ms = new MemoryStream(bytes);

                Image image = Image.FromStream(ms);
                Graphics graphics = Graphics.FromImage(image);
                Pen pen = new Pen(Color.Cyan, 3);
                Font font = new Font("Arial", 16);
                SolidBrush brush = new SolidBrush(Color.Black);

                foreach (var detectedObject in analysis.Objects)
                {
                    // Print object name
                    Console.WriteLine($" -{detectedObject.ObjectProperty} (confidence: {detectedObject.Confidence.ToString("P")})");

                    // Draw object bounding box
                    var r = detectedObject.Rectangle;
                    Rectangle rect = new Rectangle(r.X, r.Y, r.W, r.H);
                    graphics.DrawRectangle(pen, rect);
                    graphics.DrawString(detectedObject.ObjectProperty, font, brush, r.X, r.Y);

                }
                // Save annotated image
                String output_file = "objects.jpg";
                image.Save(output_file);
                Console.WriteLine("  Results saved in " + output_file);
            }


            // Get moderation ratings
            string ratings = $"Ratings:\n -Adult: {analysis.Adult.IsAdultContent}\n -Racy: {analysis.Adult.IsRacyContent}\n -Gore: {analysis.Adult.IsGoryContent}";
            Console.WriteLine(ratings);


        }

        static async Task GetThumbnail(string imageFile)
        {
            Console.WriteLine("Generating thumbnail");

            // Generate a thumbnail

            // Get thumbnail data
            var thumbnailStream = await cvClient.GenerateThumbnailAsync(100, 100, imageFile, true);

            // Save thumbnail image
            string thumbnailFileName = "thumbnail.png";
            using (Stream thumbnailFile = File.Create(thumbnailFileName))
            {
                thumbnailStream.CopyTo(thumbnailFile);
            }

            Console.WriteLine($"Thumbnail saved in {thumbnailFileName}");

        }

    }

}