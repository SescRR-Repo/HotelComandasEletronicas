# ?? Hotel Comandas Eletrônicas v2.0

Sistema de gestão de comandas eletrônicas para hotéis, desenvolvido especialmente para a **Instância Ecológica do Tepequém**. Este sistema local permite o controle completo de consumos dos hóspedes sem dependência de internet.

## ?? Índice

- [Sobre o Projeto](#sobre-o-projeto)
- [Características Principais](#características-principais)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Arquitetura do Sistema](#arquitetura-do-sistema)
- [Instalação e Configuração](#instalação-e-configuração)
- [Estrutura do Banco de Dados](#estrutura-do-banco-de-dados)
- [Perfis de Usuários](#perfis-de-usuários)
- [Funcionalidades](#funcionalidades)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Uso do Sistema](#uso-do-sistema)
- [Status do Desenvolvimento](#status-do-desenvolvimento)
- [Próximas Etapas](#próximas-etapas)

## ?? Sobre o Projeto

O **Hotel Comandas Eletrônicas** é um sistema de gestão desenvolvido em ASP.NET Core 8 MVC que substitui o controle manual de consumos por um sistema digital robusto e confiável. O sistema funciona completamente offline, sendo ideal para locais com conectividade limitada.

### ?? Objetivos
- Digitalizar o controle de comandas de consumo
- Eliminar erros de cálculo manual
- Fornecer transparência total aos hóspedes
- Criar auditoria completa de todas as operações
- Funcionar sem dependência de internet

## ? Características Principais
?
- ?? **Sistema Local**: Funciona sem internet
- ?? **Multi-usuário**: Diferentes perfis com permissões específicas
- ?? **Responsivo**: Interface adaptada para mobile e desktop
- ?? **Transparente**: Hóspedes podem consultar seus extratos
- ?? **Auditoria**: Log completo de todas as operações
- ?? **Rápido**: Interface otimizada para operação ágil

## ?? Tecnologias Utilizadas

### Backend
- **ASP.NET Core 8 MVC** - Framework web
- **Entity Framework Core 8** - ORM para banco de dados
- **SQL Server Express** - Banco de dados
- **BCrypt.Net** - Criptografia de senhas
- **FluentValidation** - Validação de dados
- **Serilog** - Sistema de logs

### Frontend
- **Bootstrap 5** - Framework CSS responsivo
- **FontAwesome** - Ícones
- **jQuery** - Interações JavaScript
- **Razor Pages** - Engine de templates

### Ferramentas de Desenvolvimento
- **Visual Studio 2022** - IDE
- **.NET 8 SDK** - Kit de desenvolvimento
- **SQL Server Express** - Banco de dados local

## ?? Arquitetura do Sistema

```
HotelComandasEletronicas/
??? Controllers/         # Controladores MVC
??? Models/             # Entidades do banco de dados
??? Data/               # Contexto do Entity Framework
??? Views/              # Views Razor
??? wwwroot/            # Arquivos estáticos
??? Helpers/            # Classes auxiliares (planejado)
??? Services/           # Lógica de negócio (planejado)
??? Validators/         # Validadores (planejado)
??? Repositories/       # Repositórios (planejado)
```

## ?? Estrutura do Banco de Dados

### Tabelas Principais

#### ?? USUARIOS_SISTEMA
Gerencia os usuários do sistema com diferentes perfis de acesso.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| ID | int | Chave primária |
| Nome | string(100) | Nome completo do usuário |
| Login | string(50) | Login único (ex: anacclara01) |
| CodigoID | string(2) | Código numérico (ex: 01, 03, 18) |
| Perfil | string(20) | Garçom, Recepção, Supervisor |
| Senha | string(255) | Hash criptografado |
| Status | bool | Ativo/Inativo |

#### ?? REGISTROS_HOSPEDE
Controla os registros de hóspedes e seus quartos.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| ID | int | Chave primária |
| NumeroQuarto | string(20) | 101, 205A, Chalé 3 |
| NomeCliente | string(100) | Nome do hóspede |
| TelefoneCliente | string(20) | Telefone para identificação |
| ValorGastoTotal | decimal(10,2) | Total consumido (calculado) |
| Status | string(20) | Ativo, Finalizado |

#### ?? PRODUTOS
Catálogo de produtos disponíveis para consumo.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| ID | int | Chave primária |
| Descricao | string(100) | Nome do produto |
| Valor | decimal(10,2) | Preço unitário |
| Categoria | string(30) | Bebidas, Comidas, Serviços |
| Status | bool | Ativo/Inativo |

#### ?? LANCAMENTOS_CONSUMO
Registra todos os consumos realizados pelos hóspedes.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| ID | int | Chave primária |
| DataHoraLancamento | datetime | Momento do lançamento |
| RegistroHospedeID | int | FK para registro do hóspede |
| ProdutoID | int | FK para produto |
| Quantidade | decimal(8,2) | Quantidade consumida |
| ValorUnitario | decimal(10,2) | Preço no momento |
| ValorTotal | decimal(10,2) | Quantidade × Valor |
| CodigoUsuarioLancamento | string(2) | Quem lançou |
| Status | string(20) | Ativo, Cancelado |

#### ?? LOGS_SISTEMA
Auditoria completa de todas as operações do sistema.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| ID | int | Chave primária |
| DataHora | datetime | Momento da ação |
| CodigoUsuario | string(50) | Usuário responsável |
| Acao | string(50) | Login, Registro, Lançamento, etc |
| Tabela | string(50) | Tabela afetada |
| DetalhesAntes | text | Estado anterior (JSON) |
| DetalhesDepois | text | Estado posterior (JSON) |

## ?? Perfis de Usuários

### ?? Garçom
- **Acesso**: Sem login, apenas código (ex: 18)
- **Permissões**: 
  - Lançar consumos
  - Consultar próprios lançamentos
- **Interface**: `/lancamento`

### ?? Recepção
- **Acesso**: Login + senha (ex: anacclara01)
- **Permissões**:
  - Registrar hóspedes
  - Cancelar lançamentos
  - Consultar extratos
  - Finalizar comandas
- **Interface**: Login necessário

### ?? Supervisor
- **Acesso**: Login + senha (ex: mariasilva01)
- **Permissões**:
  - Todas as funcionalidades
  - Cadastrar usuários e produtos
  - Relatórios gerenciais
  - Configurações do sistema
- **Interface**: Painel administrativo completo

### ????? Cliente (Hóspede)
- **Acesso**: Sem login, consulta por quarto ou nome+telefone
- **Permissões**:
  - Consultar próprio extrato
  - Visualizar histórico de consumos
- **Interface**: `/consulta`

## ?? Instalação e Configuração

### Pré-requisitos
```bash
- .NET 8 SDK
- SQL Server Express 2019 ou superior
- Visual Studio 2022 (recomendado)
```

### Passo a Passo

1. **Clone o repositório**
```bash
git clone [url-do-repositorio]
cd HotelComandasEletronicas
```

2. **Configure a string de conexão**
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=HotelComandasDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

3. **Instale as dependências**
```bash
dotnet restore
```

4. **Execute o projeto**
```bash
dotnet run
```

5. **Acesse o sistema**
- Dashboard: `https://localhost:5001`
- Garçom: `https://localhost:5001/lancamento`
- Cliente: `https://localhost:5001/consulta`

### Dados Iniciais

O sistema cria automaticamente:

**Usuários:**
- Supervisor: `mariasilva01` (senha: 123456)
- Recepção: `anacclara01` (senha: 123456)
- Garçom: Código `18` (João Santos)

**Produtos:**
- Bebidas: Água (R$ 3,50), Refrigerante (R$ 5,00), Cerveja (R$ 8,00)
- Comidas: Sanduíche (R$ 12,00), Batata (R$ 15,00), Hambúrguer (R$ 18,00)
- Serviços: Toalha Extra (R$ 10,00), Serviço de Quarto (R$ 25,00)

## ?? Uso do Sistema

### Para Garçons
1. Acesse `/lancamento`
2. Digite o código do garçom (ex: 18)
3. Selecione o quarto do hóspede
4. Escolha os produtos e quantidades
5. Confirme o lançamento

### Para Hóspedes
1. Acesse `/consulta`
2. Digite o número do quarto OU nome + telefone
3. Visualize o extrato completo
4. Acompanhe o total gasto em tempo real

### Para Recepção
1. Faça login com usuário e senha
2. Registre novos hóspedes
3. Consulte e cancele lançamentos se necessário
4. Finalize comandas no checkout

### Para Supervisores
1. Faça login com privilégios administrativos
2. Cadastre novos usuários e produtos
3. Acesse relatórios gerenciais
4. Configure parâmetros do sistema

## ?? Status do Desenvolvimento

### ? ETAPA 1 - Configuração Base (CONCLUÍDA)
- [x] Projeto ASP.NET Core 8 MVC
- [x] Entity Framework Core 8 configurado
- [x] 5 modelos de dados baseados no DER
- [x] Banco de dados com criação automática
- [x] Layout responsivo com Bootstrap 5
- [x] Sistema de logging com auditoria
- [x] Dados iniciais populados automaticamente

### ?? ETAPA 2 - Autenticação e Validação (EM ANDAMENTO)
- [ ] Sistema de autenticação por sessão
- [ ] Validação de códigos de garçom
- [ ] Middleware de autorização
- [ ] Criptografia de senhas com BCrypt
- [ ] Validação de formulários

### ?? ETAPA 3 - Funcionalidades Core (PLANEJADA)
- [ ] Lançamento de consumos (interface garçom)
- [ ] Consulta de extratos (interface cliente)
- [ ] Registro de hóspedes (interface recepção)
- [ ] Cancelamento de lançamentos
- [ ] Cálculo automático de totais

### ?? ETAPA 4 - Relatórios e Gestão (PLANEJADA)
- [ ] Relatórios gerenciais
- [ ] Cadastro de produtos e usuários
- [ ] Dashboard administrativo
- [ ] Exportação de dados
- [ ] Backup automático

### ?? ETAPA 5 - Finalização (PLANEJADA)
- [ ] Testes automatizados
- [ ] Documentação técnica
- [ ] Manual do usuário
- [ ] Script de instalação
- [ ] Configuração para produção

## ?? Próximas Etapas

### Imediatas (Próximos 7 dias)
1. Implementar sistema de autenticação por sessão
2. Criar controllers para Usuário, Garçom e Cliente
3. Desenvolver interfaces de lançamento e consulta
4. Implementar validações de negócio

### Médio Prazo (Próximas 2 semanas)
1. Finalizar todas as funcionalidades core
2. Implementar relatórios básicos
3. Criar sistema de backup
4. Realizar testes de integração

### Longo Prazo (Próximo mês)
1. Otimizações de performance
2. Funcionalidades avançadas
3. Documentação completa
4. Deploy para produção

## ?? Contribuição

Este é um projeto específico para a Instância Ecológica do Tepequém. Para contribuições:

1. Faça fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## ?? Licença

Este projeto é proprietário e desenvolvido especificamente para o Hotel Tepequém.

## ?? Suporte

Para suporte técnico ou dúvidas sobre o sistema:

- **Email**: [email-de-suporte]
- **Telefone**: [telefone-de-suporte]
- **Localização**: Instância Ecológica do Tepequém

---

**Hotel Comandas Eletrônicas v2.0** - Sistema local sem dependência de internet
*Desenvolvido com ?? para a Instância Ecológica do Tepequém*