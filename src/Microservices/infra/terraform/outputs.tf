output "control_plane_public_ip" {
  value = aws_eip.control_plane.public_ip
}

output "control_plane_private_ip" {
  value = aws_instance.control_plane.private_ip
}

output "edge_public_ip" {
  value = aws_eip.edge.public_ip
}

# certbot 核發憑證要用的網域,填進 infra/k8s/.env 的 EDGE_PUBLIC_DNS
output "edge_public_dns" {
  value = aws_eip.edge.public_dns
}

output "edge_private_ip" {
  value = aws_instance.edge.private_ip
}

output "worker_private_ips" {
  value = aws_instance.worker[*].private_ip
}

output "worker_public_ips" {
  value = aws_instance.worker[*].public_ip
}
