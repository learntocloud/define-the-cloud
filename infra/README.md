# Learn to Cloud (Define the Cloud) IaC Files

This Terraform configuration creates the following Azure resources:

- Resource Group: A new resource group to contain the Azure Function, CosmosDB, and related resources
- Azure Function: A .NET 8 out-of-process function hosted on a Linux Consumption Plan
- Application Insights: Integrated with the Azure Function for logging and monitoring
- Storage Account: Used by the Azure Function for state management and file storage
- CosmosDB Account: A serverless CosmosDB instance with a NoSQL API, containing one database and two collections (with id as the partition key)

## Prerequisites

Before you begin, ensure you have the following:

- Azure CLI installed and authenticated
- Terraform installed
- Access to an Azure subscription

## Deploying Resources

Follow the steps below to deploy the infrastructure using Terraform:

### Step 1: Initialize Terraform

Initialize the Terraform working directory. This step downloads the required providers and modules.
```
terraform init
```

### Step 2: Set Up Environment Variables

Create a `terraform.tfvars` file to store the variable values required by the project. Here is an example of the required variables:

``` Bash
location                 = "East US"
resource_group_name      = "ltc-definethecloud-rg"
storage_account_name     = "ltclinuxazf"
account_tier             = "Standard"
account_replication_type = "LRS"
app_service_plan_name = "ltc-asp"
function_app_name = "ltc-linux-azf"
cosmosdb_account_name = "ltc-cosmosdb"
```
### Step 3: Review/Plan the Changes

Review the infrastructure changes before applying them. This command will show you what resources Terraform will create and how they will be configured.

```
terraform plan
```

### Step 4: Apply the changes

To deploy the Azure infrastructure, run:
``` 
terraform apply
```

## Clean Up Resources

To destroy the resources created by this project, run: 

```
terraform destroy
```