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
}

data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "demo" {
  name     = "rg-firewall-demo"
  location = "germanywestcentral"
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

resource "azurerm_role_assignment" "firewall_contributor_current" {
  scope                = azurerm_firewall.demo.id
  role_definition_name = "Contributor"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_firewall_network_rule_collection" "network_rules" {
  name                = "demo-network-rules"
  azure_firewall_name = azurerm_firewall.demo.name
  resource_group_name = azurerm_resource_group.demo.name
  priority            = 100
  action              = "Allow"

  rule {
    name = "allow-https"

    source_addresses = [
      "10.1.0.0/24",
      "10.1.1.0/24",
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

  rule {
    name = "allow-dns-udp"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "53",
    ]

    destination_addresses = [
      "168.63.129.16",
    ]

    protocols = [
      "UDP",
    ]
  }

  rule {
    name = "allow-dns-tcp"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "53",
    ]

    destination_addresses = [
      "168.63.129.16",
    ]

    protocols = [
      "TCP",
    ]
  }

  rule {
    name = "allow-ntp"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "123",
    ]

    destination_addresses = [
      "0.0.0.0/0",
    ]

    protocols = [
      "UDP",
    ]
  }

  rule {
    name = "allow-smtp-submission"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "587",
    ]

    destination_addresses = [
      "0.0.0.0/0",
    ]

    protocols = [
      "TCP",
    ]
  }

  rule {
    name = "allow-ssh-to-jumpbox"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "22",
    ]

    destination_addresses = [
      "10.2.1.10",
    ]

    protocols = [
      "TCP",
    ]
  }

  rule {
    name = "allow-rdp-to-jumpbox"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "3389",
    ]

    destination_addresses = [
      "10.2.1.11",
    ]

    protocols = [
      "TCP",
    ]
  }

  rule {
    name = "allow-sql-to-shared-database"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "1433",
    ]

    destination_addresses = [
      "10.2.2.20",
    ]

    protocols = [
      "TCP",
    ]
  }

  rule {
    name = "allow-redis-to-cache"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "6380",
    ]

    destination_addresses = [
      "10.2.2.30",
    ]

    protocols = [
      "TCP",
    ]
  }

  rule {
    name = "allow-syslog-to-logging"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "514",
    ]

    destination_addresses = [
      "10.2.3.10",
    ]

    protocols = [
      "UDP",
    ]
  }

  rule {
    name = "allow-https-via-corporate-proxy"

    source_addresses = [
      "10.1.0.0/24",
    ]

    destination_ports = [
      "3128",
    ]

    destination_addresses = [
      "203.0.113.10",
    ]

    protocols = [
      "TCP",
    ]
  }
}
