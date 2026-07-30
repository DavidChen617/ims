variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "ap-northeast-1"
}

variable "project" {
  description = "資源命名前綴"
  type        = string
  default     = "ims"
}

variable "vpc_cidr" {
  type    = string
  default = "10.0.0.0/16"
}

variable "public_subnet_cidr" {
  type    = string
  default = "10.0.1.0/24"
}

variable "key_name" {
  description = "既有的 EC2 key pair 名稱，用來 SSH 進節點"
  type        = string
}

variable "admin_cidrs" {
  description = "額外允許 SSH(22)/kube-apiserver(6443) 存取的來源 CIDR。apply 當下的來源 IP 一律會自動加進去，這裡只用來補其他固定 IP（辦公室之類），沒有的話留空即可"
  type        = list(string)
  default     = []
}

variable "control_plane_instance_type" {
  type    = string
  default = "t4g.medium"
}

variable "edge_instance_type" {
  type    = string
  default = "t4g.medium"
}

variable "worker_instance_type" {
  type    = string
  default = "t4g.large"
}

variable "worker_count" {
  type    = number
  default = 2
}
