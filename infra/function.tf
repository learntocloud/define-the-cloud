# Create a Storage Account
resource "azurerm_storage_account" "storage_account" {
  name                     = var.storage_account_name
  resource_group_name      = azurerm_resource_group.resource_group.name
  location                 = var.location
  account_tier             = var.account_tier
  account_replication_type = var.account_replication_type
  account_kind             = "StorageV2"
  https_traffic_only_enabled = "true"
  infrastructure_encryption_enabled = "true"
}

# Create an App Service Plan
resource "azurerm_service_plan" "service_plan" {
  name                = var.app_service_plan_name
  resource_group_name = azurerm_resource_group.resource_group.name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "Y1"
}

# Create an Azure Linux Function App
resource "azurerm_linux_function_app" "example" {
  name                       = var.function_app_name
  resource_group_name        = azurerm_resource_group.resource_group.name
  location                   = var.location
  storage_account_name       = var.storage_account_name
  storage_account_access_key = azurerm_storage_account.storage_account.primary_access_key
  service_plan_id            = azurerm_service_plan.service_plan.id

site_config {
  application_stack {
      dotnet_version = "8.0"
      use_dotnet_isolated_runtime = "true"
  }
}
}