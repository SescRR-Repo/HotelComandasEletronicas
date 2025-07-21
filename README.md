# ?? Hotel Comandas Eletr�nicas v2.0

Sistema de gest�o de comandas eletr�nicas para hot�is, desenvolvido especialmente para a **Inst�ncia Ecol�gica do Tepequ�m**. Este sistema local permite o controle completo de consumos dos h�spedes sem depend�ncia de internet.

## ?? �ndice

- [Sobre o Projeto](#sobre-o-projeto)
- [Caracter�sticas Principais](#caracter�sticas-principais)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Arquitetura do Sistema](#arquitetura-do-sistema)
- [Instala��o e Configura��o](#instala��o-e-configura��o)
- [Estrutura do Banco de Dados](#estrutura-do-banco-de-dados)
- [Perfis de Usu�rios](#perfis-de-usu�rios)
- [Funcionalidades](#funcionalidades)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Uso do Sistema](#uso-do-sistema)
- [Status do Desenvolvimento](#status-do-desenvolvimento)
- [Pr�ximas Etapas](#pr�ximas-etapas)

## ?? Sobre o Projeto

O **Hotel Comandas Eletr�nicas** � um sistema de gest�o desenvolvido em ASP.NET Core 8 MVC que substitui o controle manual de consumos por um sistema digital robusto e confi�vel. O sistema funciona completamente offline, sendo ideal para locais com conectividade limitada.

### ?? Objetivos
- Digitalizar o controle de comandas de consumo
- Eliminar erros de c�lculo manual
- Fornecer transpar�ncia total aos h�spedes
- Criar auditoria completa de todas as opera��es
- Funcionar sem depend�ncia de internet

## ? Caracter�sticas Principais
?
- ?? **Sistema Local**: Funciona sem internet
- ?? **Multi-usu�rio**: Diferentes perfis com permiss�es espec�ficas
- ?? **Responsivo**: Interface adaptada para mobile e desktop
- ?? **Transparente**: H�spedes podem consultar seus extratos
- ?? **Auditoria**: Log completo de todas as opera��es
- ?? **R�pido**: Interface otimizada para opera��o �gil

## ?? Tecnologias Utilizadas

### Backend
- **ASP.NET Core 8 MVC** - Framework web
- **Entity Framework Core 8** - ORM para banco de dados
- **SQL Server Express** - Banco de dados
- **BCrypt.Net** - Criptografia de senhas
- **FluentValidation** - Valida��o de dados
- **Serilog** - Sistema de logs

### Frontend
- **Bootstrap 5** - Framework CSS responsivo
- **FontAwesome** - �cones
- **jQuery** - Intera��es JavaScript
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
??? wwwroot/            # Arquivos est�ticos
??? Helpers/            # Classes auxiliares (planejado)
??? Services/           # L�gica de neg�cio (planejado)
??? Validators/         # Validadores (planejado)
??? Repositories/       # Reposit�rios (planejado)
```

## ?? Estrutura do Banco de Dados

### Tabelas Principais

#### ?? USUARIOS_SISTEMA
Gerencia os usu�rios do sistema com diferentes perfis de acesso.

| Campo | Tipo | Descri��o |
|-------|------|-----------|
| ID | int | Chave prim�ria |
| Nome | string(100) | Nome completo do usu�rio |
| Login | string(50) | Login �nico (ex: anacclara01) |
| CodigoID | string(2) | C�digo num�rico (ex: 01, 03, 18) |
| Perfil | string(20) | Gar�om, Recep��o, Supervisor |
| Senha | string(255) | Hash criptografado |
| Status | bool | Ativo/Inativo |

#### ?? REGISTROS_HOSPEDE
Controla os registros de h�spedes e seus quartos.

| Campo | Tipo | Descri��o |
|-------|------|-----------|
| ID | int | Chave prim�ria |
| NumeroQuarto | string(20) | 101, 205A, Chal� 3 |
| NomeCliente | string(100) | Nome do h�spede |
| TelefoneCliente | string(20) | Telefone para identifica��o |
| ValorGastoTotal | decimal(10,2) | Total consumido (calculado) |
| Status | string(20) | Ativo, Finalizado |

#### ?? PRODUTOS
Cat�logo de produtos dispon�veis para consumo.

| Campo | Tipo | Descri��o |
|-------|------|-----------|
| ID | int | Chave prim�ria |
| Descricao | string(100) | Nome do produto |
| Valor | decimal(10,2) | Pre�o unit�rio |
| Categoria | string(30) | Bebidas, Comidas, Servi�os |
| Status | bool | Ativo/Inativo |

#### ?? LANCAMENTOS_CONSUMO
Registra todos os consumos realizados pelos h�spedes.

| Campo | Tipo | Descri��o |
|-------|------|-----------|
| ID | int | Chave prim�ria |
| DataHoraLancamento | datetime | Momento do lan�amento |
| RegistroHospedeID | int | FK para registro do h�spede |
| ProdutoID | int | FK para produto |
| Quantidade | decimal(8,2) | Quantidade consumida |
| ValorUnitario | decimal(10,2) | Pre�o no momento |
| ValorTotal | decimal(10,2) | Quantidade � Valor |
| CodigoUsuarioLancamento | string(2) | Quem lan�ou |
| Status | string(20) | Ativo, Cancelado |

#### ?? LOGS_SISTEMA
Auditoria completa de todas as opera��es do sistema.

| Campo | Tipo | Descri��o |
|-------|------|-----------|
| ID | int | Chave prim�ria |
| DataHora | datetime | Momento da a��o |
| CodigoUsuario | string(50) | Usu�rio respons�vel |
| Acao | string(50) | Login, Registro, Lan�amento, etc |
| Tabela | string(50) | Tabela afetada |
| DetalhesAntes | text | Estado anterior (JSON) |
| DetalhesDepois | text | Estado posterior (JSON) |

## ?? Perfis de Usu�rios

### ?? Gar�om
- **Acesso**: Sem login, apenas c�digo (ex: 18)
- **Permiss�es**: 
  - Lan�ar consumos
  - Consultar pr�prios lan�amentos
- **Interface**: `/lancamento`

### ?? Recep��o
- **Acesso**: Login + senha (ex: anacclara01)
- **Permiss�es**:
  - Registrar h�spedes
  - Cancelar lan�amentos
  - Consultar extratos
  - Finalizar comandas
- **Interface**: Login necess�rio

### ?? Supervisor
- **Acesso**: Login + senha (ex: mariasilva01)
- **Permiss�es**:
  - Todas as funcionalidades
  - Cadastrar usu�rios e produtos
  - Relat�rios gerenciais
  - Configura��es do sistema
- **Interface**: Painel administrativo completo

### ????? Cliente (H�spede)
- **Acesso**: Sem login, consulta por quarto ou nome+telefone
- **Permiss�es**:
  - Consultar pr�prio extrato
  - Visualizar hist�rico de consumos
- **Interface**: `/consulta`

## ?? Instala��o e Configura��o

### Pr�-requisitos
```bash
- .NET 8 SDK
- SQL Server Express 2019 ou superior
- Visual Studio 2022 (recomendado)
```

### Passo a Passo

1. **Clone o reposit�rio**
```bash
git clone [url-do-repositorio]
cd HotelComandasEletronicas
```

2. **Configure a string de conex�o**
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=HotelComandasDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

3. **Instale as depend�ncias**
```bash
dotnet restore
```

4. **Execute o projeto**
```bash
dotnet run
```

5. **Acesse o sistema**
- Dashboard: `https://localhost:5001`
- Gar�om: `https://localhost:5001/lancamento`
- Cliente: `https://localhost:5001/consulta`

### Dados Iniciais

O sistema cria automaticamente:

**Usu�rios:**
- Supervisor: `mariasilva01` (senha: 123456)
- Recep��o: `anacclara01` (senha: 123456)
- Gar�om: C�digo `18` (Jo�o Santos)

**Produtos:**
- Bebidas: �gua (R$ 3,50), Refrigerante (R$ 5,00), Cerveja (R$ 8,00)
- Comidas: Sandu�che (R$ 12,00), Batata (R$ 15,00), Hamb�rguer (R$ 18,00)
- Servi�os: Toalha Extra (R$ 10,00), Servi�o de Quarto (R$ 25,00)

## ?? Uso do Sistema

### Para Gar�ons
1. Acesse `/lancamento`
2. Digite o c�digo do gar�om (ex: 18)
3. Selecione o quarto do h�spede
4. Escolha os produtos e quantidades
5. Confirme o lan�amento

### Para H�spedes
1. Acesse `/consulta`
2. Digite o n�mero do quarto OU nome + telefone
3. Visualize o extrato completo
4. Acompanhe o total gasto em tempo real

### Para Recep��o
1. Fa�a login com usu�rio e senha
2. Registre novos h�spedes
3. Consulte e cancele lan�amentos se necess�rio
4. Finalize comandas no checkout

### Para Supervisores
1. Fa�a login com privil�gios administrativos
2. Cadastre novos usu�rios e produtos
3. Acesse relat�rios gerenciais
4. Configure par�metros do sistema

## ?? Status do Desenvolvimento

### ? ETAPA 1 - Configura��o Base (CONCLU�DA)
- [x] Projeto ASP.NET Core 8 MVC
- [x] Entity Framework Core 8 configurado
- [x] 5 modelos de dados baseados no DER
- [x] Banco de dados com cria��o autom�tica
- [x] Layout responsivo com Bootstrap 5
- [x] Sistema de logging com auditoria
- [x] Dados iniciais populados automaticamente

### ?? ETAPA 2 - Autentica��o e Valida��o (EM ANDAMENTO)
- [ ] Sistema de autentica��o por sess�o
- [ ] Valida��o de c�digos de gar�om
- [ ] Middleware de autoriza��o
- [ ] Criptografia de senhas com BCrypt
- [ ] Valida��o de formul�rios

### ?? ETAPA 3 - Funcionalidades Core (PLANEJADA)
- [ ] Lan�amento de consumos (interface gar�om)
- [ ] Consulta de extratos (interface cliente)
- [ ] Registro de h�spedes (interface recep��o)
- [ ] Cancelamento de lan�amentos
- [ ] C�lculo autom�tico de totais

### ?? ETAPA 4 - Relat�rios e Gest�o (PLANEJADA)
- [ ] Relat�rios gerenciais
- [ ] Cadastro de produtos e usu�rios
- [ ] Dashboard administrativo
- [ ] Exporta��o de dados
- [ ] Backup autom�tico

### ?? ETAPA 5 - Finaliza��o (PLANEJADA)
- [ ] Testes automatizados
- [ ] Documenta��o t�cnica
- [ ] Manual do usu�rio
- [ ] Script de instala��o
- [ ] Configura��o para produ��o

## ?? Pr�ximas Etapas

### Imediatas (Pr�ximos 7 dias)
1. Implementar sistema de autentica��o por sess�o
2. Criar controllers para Usu�rio, Gar�om e Cliente
3. Desenvolver interfaces de lan�amento e consulta
4. Implementar valida��es de neg�cio

### M�dio Prazo (Pr�ximas 2 semanas)
1. Finalizar todas as funcionalidades core
2. Implementar relat�rios b�sicos
3. Criar sistema de backup
4. Realizar testes de integra��o

### Longo Prazo (Pr�ximo m�s)
1. Otimiza��es de performance
2. Funcionalidades avan�adas
3. Documenta��o completa
4. Deploy para produ��o

## ?? Contribui��o

Este � um projeto espec�fico para a Inst�ncia Ecol�gica do Tepequ�m. Para contribui��es:

1. Fa�a fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudan�as (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## ?? Licen�a

Este projeto � propriet�rio e desenvolvido especificamente para o Hotel Tepequ�m.

## ?? Suporte

Para suporte t�cnico ou d�vidas sobre o sistema:

- **Email**: [email-de-suporte]
- **Telefone**: [telefone-de-suporte]
- **Localiza��o**: Inst�ncia Ecol�gica do Tepequ�m

---

**Hotel Comandas Eletr�nicas v2.0** - Sistema local sem depend�ncia de internet
*Desenvolvido com ?? para a Inst�ncia Ecol�gica do Tepequ�m*