# apply 當下的來源 IP 自動抓,不用每次 IP 變了就手動改 tfvars。
data "http" "my_ip" {
  url = "https://checkip.amazonaws.com"
}

locals {
  admin_cidrs = concat(var.admin_cidrs, ["${chomp(data.http.my_ip.response_body)}/32"])
}

# 四台共用：cluster 內部互打全開（etcd/kubelet/apiserver/Calico VXLAN 等埠很多且隨版本變動，
# 同一顆 SG 自我參照直接放行比逐一列埠務實），對外只開 SSH 與 kube-apiserver，且僅限 admin_cidrs。
resource "aws_security_group" "cluster" {
  name        = "${var.project}-cluster"
  description = "k8s cluster nodes: internal all-open + admin SSH/6443"
  vpc_id      = aws_vpc.main.id

  tags = {
    Name = "${var.project}-cluster"
  }
}

resource "aws_security_group_rule" "cluster_internal_all" {
  type                     = "ingress"
  from_port                = 0
  to_port                  = 0
  protocol                 = "-1"
  security_group_id        = aws_security_group.cluster.id
  source_security_group_id = aws_security_group.cluster.id
}

resource "aws_security_group_rule" "ssh_from_admin" {
  type              = "ingress"
  from_port         = 22
  to_port           = 22
  protocol          = "tcp"
  security_group_id = aws_security_group.cluster.id
  cidr_blocks       = local.admin_cidrs
}

resource "aws_security_group_rule" "kube_api_from_admin" {
  type              = "ingress"
  from_port         = 6443
  to_port           = 6443
  protocol          = "tcp"
  security_group_id = aws_security_group.cluster.id
  cidr_blocks       = local.admin_cidrs
}

resource "aws_security_group_rule" "cluster_egress_all" {
  type              = "egress"
  from_port         = 0
  to_port           = 0
  protocol          = "-1"
  security_group_id = aws_security_group.cluster.id
  cidr_blocks       = ["0.0.0.0/0"]
}

# 只掛在 edge node：唯一對整個公網開放 80/443 的節點。
resource "aws_security_group" "edge_public" {
  name        = "${var.project}-edge-public"
  description = "edge node: public ingress 80/443"
  vpc_id      = aws_vpc.main.id

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "${var.project}-edge-public"
  }
}
