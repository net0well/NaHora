# Modelos ONNX

Os arquivos de modelo **não são versionados**: são binários grandes, com licença
própria, e alguns têm restrição de uso. Baixe com:

```powershell
.\scripts\download-models.ps1
```

A aplicação verifica o SHA-256 de cada arquivo na inicialização e **recusa
iniciar** se algum divergir ou estiver ausente. Um modelo trocado produziria
embeddings errados sem gerar nenhum erro visível — o reconhecimento passaria a
falhar sem explicação.

---

## Modelos utilizados

| Arquivo | Etapa | Origem | Licença | Uso comercial |
|---------|-------|--------|---------|:-------------:|
| `face_detection_yunet_2023mar.onnx` | Detecção facial e landmarks | [OpenCV Zoo](https://github.com/opencv/opencv_zoo/tree/main/models/face_detection_yunet) | Apache-2.0 | Permitido |
| `face_recognition_sface_2021dec.onnx` | Geração de embeddings | [OpenCV Zoo](https://github.com/opencv/opencv_zoo/tree/main/models/face_recognition_sface) | Apache-2.0 | Permitido |
| `minifasnet_v1.onnx` | Anti-spoofing passivo | [Silent-Face-Anti-Spoofing](https://github.com/minivision-ai/Silent-Face-Anti-Spoofing) | Apache-2.0 no código; **pesos sem licença explícita** | Risco aceito — ver abaixo |
| `minifasnet_v2.onnx` | Anti-spoofing passivo | idem | idem | idem |

Os hashes SHA-256 esperados ficam em `scripts/download-models.ps1` e são a fonte
de verdade da verificação na inicialização.

---

## Modelos deliberadamente rejeitados

| Modelo | Motivo |
|--------|--------|
| InsightFace `buffalo_l` | Os pesos são *non-commercial research purposes only*. O código é MIT, mas é o peso que faz o reconhecimento. Uso comercial exige contrato pago com a InsightFace |
| ArcFace R50 / R100 | Mesma restrição — publicados no model zoo do InsightFace |
| SCRFD | Mesma restrição. Substituído por YuNet |

---

## Risco conhecido — pesos do MiniFASNet

O código do Silent-Face-Anti-Spoofing é Apache-2.0, mas **os pesos não trazem
licença própria** e foram treinados sobre o CelebA-Spoof (que é *research only*)
somados a dados proprietários da Minivision. Licença permissiva no código-fonte
não licencia pesos de proveniência indeterminada.

Este risco foi avaliado e **aceito de forma explícita** para esta fase do
produto. Mitigações em vigor:

1. O **liveness ativo por desafio** — que não usa modelo de terceiros e valida a
   sequência temporal dos landmarks do YuNet — é obrigatório, não opcional. Se o
   MiniFASNet precisar sair, o sistema continua funcional com segurança
   reduzida, não quebrado.
2. `ILivenessDetectionService` tem implementação composta: trocar o provedor
   passivo é mudança de registro em DI, sem tocar em handler nem no domínio.
3. Substituir por modelo de proveniência limpa, ou treinado com dataset
   licenciado, é item permanente do roadmap.

---

## Antes de produção

Reverificar a licença de cada modelo e de cada peso. "Open source" não implica
uso comercial irrestrito, e a licença do repositório não necessariamente cobre os
pesos publicados nele.
