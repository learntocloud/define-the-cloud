resource "azurerm_application_insights" "main" {
  name                = "linuxazfunction-appi"
  location            = var.location
  resource_group_name = azurerm_resource_group.resource_group.name
  application_type    = "web"
  }