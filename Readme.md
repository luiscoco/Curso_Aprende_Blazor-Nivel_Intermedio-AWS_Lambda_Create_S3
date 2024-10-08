# How to create AWS Lambda (C#) for creating AWS S3 bucket

## 0. Prerequisite

Install AWS Toolkit and AWS project templates in Visual Studio 2022 Community Edition

https://aws.amazon.com/visualstudio/

![image](https://github.com/user-attachments/assets/476f1fbb-77a7-4e62-ba13-0962ed1cebe5)

https://marketplace.visualstudio.com/items?itemName=AmazonWebServices.AWSToolkitforVisualStudio2022

![image](https://github.com/user-attachments/assets/13f448b0-87e0-4dcb-bbc4-9d50d9f52265)

Log in AWS Console and create a Access Key and Secret Key

Run this command to configure the AWS account:

![image](https://github.com/user-attachments/assets/a30ca0a5-609e-4cf0-8463-5f00140414fd)

## 1. Run Visual Studio and create an Empty AWS Lambda

Run Visual Studio 2022 and create a new project

![image](https://github.com/user-attachments/assets/669eb40e-31f2-48d2-82a3-483d62a433ba)

Search for **AWS Lambda** project template:

![image](https://github.com/user-attachments/assets/a4da718f-e45c-46f1-b938-9f7570608896)

Input the project name and the project location in the hard disk:

![image](https://github.com/user-attachments/assets/bfbc9666-cfae-476d-a4b9-c06d6d0f951d)

Select the **Empty Function** and press the **Finish** button

![image](https://github.com/user-attachments/assets/90a2737f-303f-4793-8a97-1003e204a2f7)

## 2. Before continue we have to verify the application is referencing .NET 6 (because nowadays is the stable version for AWS Lambda)

First we double-click on the project name **AWSLambda1.csproj** and set **.NET 6**

![image](https://github.com/user-attachments/assets/898340d5-5250-4a9e-810c-e9c26d73a5f2)

We also has to edit the **aws-lambda-tools-defaults.json** file and set **.NET 6**

![image](https://github.com/user-attachments/assets/9f903eac-70a3-47b1-8c5e-a5ac40fd922d)

## 3. Load the Nuget packages

We load the **AWSSDK.S3** package

![image](https://github.com/user-attachments/assets/ff99aa90-ee75-4c99-a87f-1280d9318f33)

## 4. Input the source code in C#

This C# code is designed to be used as an AWS Lambda function. The function handles requests from API Gateway and interacts with Amazon S3 to create a new S3 bucket

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
                context.Logger.LogLine("Starting function handler...");
                context.Logger.LogLine($"Raw input: {request.Body ?? "No body"}");

                // Check if the request body is null
                if (string.IsNullOrEmpty(request.Body))
                {
                    context.Logger.LogLine("Request body is null or empty.");
                    throw new ArgumentException("Request body cannot be null or empty.");
                }

                // Deserialize the body to extract the bucket name
                context.Logger.LogLine("Attempting to deserialize input...");
                var inputObject = JsonSerializer.Deserialize<InputObject>(request.Body);

                if (inputObject == null)
                {
                    context.Logger.LogLine("Deserialization returned null.");
                    throw new ArgumentException("Failed to deserialize the input.");
                }

                // Ensure the bucket name is retrieved correctly
                string bucketName = inputObject?.BucketName;
                context.Logger.LogLine($"Bucket name retrieved: {bucketName}");

                if (string.IsNullOrEmpty(bucketName))
                {
                    context.Logger.LogLine("Bucket name is null or empty.");
                    throw new ArgumentException("Bucket name is required.");
                }

                // Create the S3 bucket
                context.Logger.LogLine($"Attempting to create S3 bucket: {bucketName}");
                await CreateS3BucketAsync(bucketName, context);

                context.Logger.LogLine($"Bucket '{bucketName}' created successfully.");
                return $"Bucket '{bucketName}' created successfully!";
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"General error: {ex.Message}");
                context.Logger.LogLine($"Stack trace: {ex.StackTrace}");
                return $"General error: {ex.Message}";
            }
        }

        private async Task CreateS3BucketAsync(string bucketName, ILambdaContext context)
        {
            try
            {
                context.Logger.LogLine("Listing existing S3 buckets...");

                // List all buckets and log the result
                var response = await _s3Client.ListBucketsAsync();

                if (response == null)
                {
                    context.Logger.LogLine("ListBucketsAsync response is null.");
                    throw new InvalidOperationException("Failed to list S3 buckets, response is null.");
                }

                if (response.Buckets == null || response.Buckets.Count == 0)
                {
                    context.Logger.LogLine("No existing buckets found.");
                }
                else
                {
                    context.Logger.LogLine($"Total buckets found: {response.Buckets.Count}");

                    // Check if the bucket already exists
                    if (response.Buckets.Any(b => b.BucketName == bucketName))
                    {
                        context.Logger.LogLine($"Bucket '{bucketName}' already exists.");
                        return;
                    }
                }

                // Create a new bucket
                context.Logger.LogLine($"Creating a new bucket: {bucketName}");
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketName,
                    BucketRegion = S3Region.EUWest3
                };

                PutBucketResponse putResponse = await _s3Client.PutBucketAsync(putBucketRequest);
                context.Logger.LogLine($"Bucket '{bucketName}' created. Request ID: {putResponse.ResponseMetadata.RequestId}");
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

Here's a breakdown of the key components:

**1. Namespace and Dependencies**:

The code uses the following libraries:

**Amazon.Lambda.Core**: Contains classes and interfaces for Lambda function execution.

**Amazon.S3.Model and Amazon.S3**: These are used to interact with Amazon S3, particularly for creating S3 buckets

**System.Text.Json**: Provides functionality for serializing and deserializing JSON input/output

**[assembly: LambdaSerializer]**: Specifies that the Lambda function will use SystemTextJson to serialize and deserialize JSON data

**2. Function Class**:

The main class, Function, defines the logic for handling API Gateway requests and interacting with S3

**IAmazonS3 _s3Client**: The IAmazonS3 interface is used to interact with S3 services. It is initialized using AmazonS3Client()

**3. FunctionHandler**:

This is the Lambda function entry point, where the logic is executed when the Lambda function is invoked by API Gateway

**Input**: The function takes an APIGatewayProxyRequest, which contains a Body (JSON input) and a Lambda context (ILambdaContext) for logging and tracking execution

**Deserialization**: The body of the request is deserialized into an InputObject to extract the S3 bucket name

**Error Handling**: If the bucket name is not provided or an exception occurs, the function logs the error and returns an error message

**4. CreateS3BucketAsync**:

This helper method is responsible for creating an S3 bucket

It first checks if the bucket already exists using _s3Client.DoesS3BucketExistAsync()

If the bucket doesn't exist, it creates a new bucket using _s3Client.PutBucketAsync()

Logging is performed throughout to track successful or failed operations

**5. InputObject Class**:

A simple class representing the expected input structure. It contains one property, BucketName, which is the name of the S3 bucket to be created

**6. APIGatewayProxyRequest Class**:

This represents the request model coming from API Gateway. The important property is Body, which holds the raw JSON data passed to the function

**Summary**: The code defines a Lambda function that Receives a JSON input via API Gateway. Extracts the name of an S3 bucket from the request. Checks if the bucket already exists and, if not, creates the bucket. Logs the operation's details and returns success or failure messages based on the outcome.

## 5. Open the Function.cs file in the containing folder and build the application

![image](https://github.com/user-attachments/assets/65e46201-771a-4385-8288-9b03c219b847)

![image](https://github.com/user-attachments/assets/db2a9a5a-d2bd-4e7f-a623-38408c22afef)

Now we type **cmd** to open the folder in the command prompt

![image](https://github.com/user-attachments/assets/c85aecf1-8ad9-4005-8b65-147730eeea55)

We run the commands:

```
dotnet clean
```

and

```
dotnet build
```

![image](https://github.com/user-attachments/assets/aa4dcf1e-2e17-4140-8e30-284f7312ce74)

## 6. Install the Amazon.Lambda.Tools Global Tool

You need to install the Amazon.Lambda.Tools package, which provides the command-line interface (CLI) extensions for deploying Lambda functions.

Run this command to install the Lambda .NET global tool:

```
dotnet tool install -g Amazon.Lambda.Tools
```

![image](https://github.com/user-attachments/assets/123c8a65-c791-4821-8315-657b9abe7db2)

After installing, you can check the installation by running:

```
dotnet lambda
```

This should show you the list of available commands for working with AWS Lambda

![image](https://github.com/user-attachments/assets/e8133dcc-cffd-4a1f-babc-52a10d330256)

## 7. Deploy the AWS Lambda

For deploying the AWS Lambda in AWS run the following command:

```
dotnet lambda deploy-function
```

We input the lambda function name **lambdacreates3bucket**, the lambda function role **lambdacreates3role** and we select the IAM Policy number **14) No policy, add permissions later**

![image](https://github.com/user-attachments/assets/7f57550f-6264-480a-90b5-79b49b04270d)

## 8. Login in AWS IAM and attach the policies to the Lambda Role

We click on the Lambda role

![image](https://github.com/user-attachments/assets/fd3975ff-fb0d-418a-9d14-d85fa46ab805)

We attach the policies for creating S3 bucket and for CloudWatch

![image](https://github.com/user-attachments/assets/1c085b7f-a80a-409a-9da5-3203608f4e3e)

We first stablish the policy for creating S3 butkets

![image](https://github.com/user-attachments/assets/7ba732ff-1c35-4bef-a6de-1edc7d7c77c4)

```json
{
	"Version": "2012-10-17",
	"Statement": [
		{
			"Effect": "Allow",
			"Action": [
				"s3:CreateBucket",
				"s3:ListAllMyBuckets",
				"s3:PutBucketAcl",
				"s3:PutBucketPolicy"
			],
			"Resource": "*"
		}
	]
}
```

![image](https://github.com/user-attachments/assets/f95b773f-e271-4eee-b334-e66fe0688ea5)

See the new policy attached to the lambda role

![image](https://github.com/user-attachments/assets/d11880af-cc17-4205-9ad1-116a15d76b24)

Then we define the policies for CloudWatch

![image](https://github.com/user-attachments/assets/d1a44093-9c2a-44fd-8998-71f6d7284d8a)

![image](https://github.com/user-attachments/assets/2741c79d-98ba-4558-a0ab-8eb21630f7e2)

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "*"
    }
  ]
}
```

![image](https://github.com/user-attachments/assets/bbe4bc15-0300-4f29-a8f4-b5b73d8b580f)

![image](https://github.com/user-attachments/assets/a61d370b-13e3-46dc-9514-8c5ac67b00e6)

## 9. Verify in AWS Console the Lambda was created

![image](https://github.com/user-attachments/assets/1d106eb4-e989-4f06-8e32-571567232304)

We click on the Lambda function name and navigate to the following page where we are going to create a Function URL fot testing

![image](https://github.com/user-attachments/assets/2c7a0c37-b833-48e4-baa7-70c911ce7eee)

Press **Create Function URL** button

Select **NONE** and press the **Save** button

![image](https://github.com/user-attachments/assets/dd6ea0f8-ddd5-4154-84c8-a6a6e9ab5cdb)

Copy the **Function URL** for testing the Lambda in Postman

![image](https://github.com/user-attachments/assets/ce2c06c6-3e19-4aa6-bc90-a1306a5c8c5d)

Run Postman 

![image](https://github.com/user-attachments/assets/453977eb-a2b9-4310-8e73-13798e048b8e)

We navigate to S3 in AWS Console and verify the new bucket was created

![image](https://github.com/user-attachments/assets/7749ebf1-93c5-4f90-bbc2-112941bab897)

## 10. See CloudWatch logs

We navigate to AWS CloudWatch logs

![image](https://github.com/user-attachments/assets/f1032829-72b8-4039-a91f-3e0595ad4e79)

We click on the log group and see the logs

![image](https://github.com/user-attachments/assets/67282690-5958-4dc6-a2be-196fe62dab73)

