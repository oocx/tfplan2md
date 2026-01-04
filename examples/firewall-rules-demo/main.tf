terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
  skip_provider_registration = true
}

resource "azurerm_resource_group" "demo" {
  name     = "rg-firewall-demo"
  location = "eastus"
}

resource "azurerm_virtual_network" "demo" {
  name                = "vnet-demo"
  address_space       = ["10.1.0.0/16"]
  location            = azurerm_resource_group.demo.location
  resource_group_name = azurerm_resource_group.demo.name
}

resource "azurerm_subnet" "firewall" {
  name                 = "AzureFirewallSubnet"
  resource_group_name  = azurerm_resource_group.demo.name
  virtual_network_name = azurerm_virtual_network.demo.name
  address_prefixes     = ["10.1.0.0/24"]
}

resource "azurerm_public_ip" "firewall" {
  name                = "pip-firewall"
  location            = azurerm_resource_group.demo.location
  resource_group_name = azurerm_resource_group.demo.name
  allocation_method   = "Static"
  sku                 = "Standard"
}

resource "azurerm_firewall" "demo" {
  name                = "demo-firewall"
  location            = azurerm_resource_group.demo.location
  resource_group_name = azurerm_resource_group.demo.name
  sku_name            = "AZFW_VNet"
  sku_tier            = "Standard"

  ip_configuration {
    name                 = "configuration"
    subnet_id            = azurerm_subnet.firewall.id
    public_ip_address_id = azurerm_public_ip.firewall.id
  }
}

resource "azurerm_firewall_network_rule_collection" "network_rules" {
  name                = "demo-network-rules"
  azure_firewall_name = azurerm_firewall.demo.name
  resource_group_name = azurerm_resource_group.demo.name
  priority            = 100
  action              = "Allow"

  rule {
    name = "allow-http"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "80",
    ]

    destination_addresses = [
      "0.0.0.0/0",
    ]

    protocols = [
      "TCP",
    ]
  }

  rule {
    name = "allow-https"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "443",
    ]

    destination_addresses = [
      "0.0.0.0/0",
    ]

    protocols = [
      "TCP",
    ]
  }
}
