# How to create AWS Lambda (C#) for creating AWS S3 bucket

## 0. Prerequisite

Install AWS Toolkit and AWS project templates in Visual Studio 2022 Community Edition

https://aws.amazon.com/visualstudio/

![image](https://github.com/user-attachments/assets/476f1fbb-77a7-4e62-ba13-0962ed1cebe5)

https://marketplace.visualstudio.com/items?itemName=AmazonWebServices.AWSToolkitforVisualStudio2022

![image](https://github.com/user-attachments/assets/13f448b0-87e0-4dcb-bbc4-9d50d9f52265)

## 1. Run Visual Studio and create an Empty AWS Lambda

Run Visual Studio 2022 and create a new project

![image](https://github.com/user-attachments/assets/669eb40e-31f2-48d2-82a3-483d62a433ba)

Search for **AWS Lambda** project template:

![image](https://github.com/user-attachments/assets/a4da718f-e45c-46f1-b938-9f7570608896)

Input the project name and the project location in the hard disk:

![image](https://github.com/user-attachments/assets/bfbc9666-cfae-476d-a4b9-c06d6d0f951d)

Select the **Empty Function** and press the **Finish** button

![image](https://github.com/user-attachments/assets/90a2737f-303f-4793-8a97-1003e204a2f7)

## 2. Load the Nuget packages

![image](https://github.com/user-attachments/assets/ff99aa90-ee75-4c99-a87f-1280d9318f33)

## 3. Input the source code in C#

```csharp
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
```




