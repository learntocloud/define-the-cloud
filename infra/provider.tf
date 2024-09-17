terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 3.0, < 4.0"
    }
  }
}

# Need to add features block below because Application Insights is a nested resource and won't delete Resource Group via terraform destroy
# Will receive an error without the added features block
provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}