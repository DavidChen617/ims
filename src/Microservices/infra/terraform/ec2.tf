resource "aws_instance" "control_plane" {
  ami                    = data.aws_ami.ubuntu_arm64.id
  instance_type          = var.control_plane_instance_type
  subnet_id              = aws_subnet.public.id
  vpc_security_group_ids = [aws_security_group.cluster.id]
  key_name               = var.key_name
  source_dest_check = false

  root_block_device {
    volume_size = 30
    volume_type = "gp3"
  }

  tags = {
    Name = "${var.project}-control-plane"
    Role = "control-plane"
  }
}

resource "aws_eip" "control_plane" {
  instance = aws_instance.control_plane.id
  domain   = "vpc"

  tags = {
    Name = "${var.project}-control-plane-eip"
  }
}

# edge：加入 cluster 當 worker，另外 taint 只跑 ingress-nginx，唯一對外開 80/443 的節點。
resource "aws_instance" "edge" {
  ami                    = data.aws_ami.ubuntu_arm64.id
  instance_type          = var.edge_instance_type
  subnet_id              = aws_subnet.public.id
  vpc_security_group_ids = [aws_security_group.cluster.id, aws_security_group.edge_public.id]
  key_name               = var.key_name
  source_dest_check      = false

  root_block_device {
    volume_size = 30
    volume_type = "gp3"
  }

  tags = {
    Name = "${var.project}-edge"
    Role = "edge"
  }
}

resource "aws_eip" "edge" {
  instance = aws_instance.edge.id
  domain   = "vpc"

  tags = {
    Name = "${var.project}-edge-eip"
  }
}

resource "aws_instance" "worker" {
  count                  = var.worker_count
  ami                    = data.aws_ami.ubuntu_arm64.id
  instance_type          = var.worker_instance_type
  subnet_id              = aws_subnet.public.id
  vpc_security_group_ids = [aws_security_group.cluster.id]
  key_name               = var.key_name
  # Calico 同一個 subnet 內預設用 CrossSubnet 模式,不會封裝 pod-to-pod 封包,
  # 這種封包的來源/目的地 IP 是 pod CIDR、跟這台實例自己的 ENI IP 對不上,
  # AWS 預設會用 source/dest check 把它們丟掉,一定要關掉。
  source_dest_check = false

  root_block_device {
    volume_size = 40
    volume_type = "gp3"
  }

  tags = {
    Name = "${var.project}-worker-${count.index + 1}"
    Role = "worker"
  }
}
