using Amazon.Lambda.Core;
using Amazon.S3.Model;
using Amazon.S3;
using System.Threading.Tasks;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambda1
{
    public class Function
    {
        private readonly IAmazonS3 _s3Client;

        public Function()
        {
            // Initialize the S3 client (Lambda automatically picks up the region from the environment)
            _s3Client = new AmazonS3Client();
        }

        public async Task<string> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Log the raw input for debugging
                context.Logger.LogLine($"Raw input: {request.Body}");

                // Deserialize the body to extract the bucket name
                var inputObject = JsonSerializer.Deserialize<InputObject>(request.Body);

                // Ensure the bucket name is retrieved correctly
                string bucketName = inputObject?.BucketName;

                if (string.IsNullOrEmpty(bucketName))
                {
                    throw new ArgumentException("Bucket name is required.");
                }

                // Create the S3 bucket
                await CreateS3BucketAsync(bucketName, context);

                return $"Bucket '{bucketName}' created successfully!";
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"General error: {ex.Message}");
                return $"General error: {ex.Message}";
            }
        }

        private async Task CreateS3BucketAsync(string bucketName, ILambdaContext context)
        {
            try
            {
                // Check if the bucket already exists
                if (await _s3Client.DoesS3BucketExistAsync(bucketName))
                {
                    context.Logger.LogLine($"Bucket '{bucketName}' already exists.");
                    return;
                }

                // Create a new bucket
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketName,
                    BucketRegion = S3Region.EUWest3
                };

                PutBucketResponse response = await _s3Client.PutBucketAsync(putBucketRequest);

                context.Logger.LogLine($"Bucket '{bucketName}' created. Request ID: {response.ResponseMetadata.RequestId}");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Failed to create bucket: {ex.Message}");
                throw;
            }
        }
    }

    // Define a class to represent the input
    public class InputObject
    {
        public string BucketName { get; set; }
    }

    // API Gateway Proxy Request model
    public class APIGatewayProxyRequest
    {
        public string Body { get; set; }
        public bool IsBase64Encoded { get; set; }
        // Add other fields from API Gateway if necessary
    }

}
