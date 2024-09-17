variable "location" {
  description = "The Azure Region in which all resources in this example should be created."
  type        = string
}

variable "resource_group_name" {
  description = "The name of the resource group in which to create the Storage Account."
  type        = string
}

variable "storage_account_name" {
  description = "The name of the Storage Account to create"
  type        = string
}

variable "account_tier" {
  description = "The tier of Storage Account to create"
  type        = string
}

variable "account_replication_type" {
  description = "The replication type of Storage Account to create"
  type        = string
}

variable "app_service_plan_name" {
  description = "The name of the App Service Plan to create"
  type        = string
}

variable "function_app_name" {
  description = "The name of the Linux Function App to create"
  type        = string
}

variable "cosmosdb_account_name" {
  description = "The name of a CosmosDB account to create"
  type        = string
}