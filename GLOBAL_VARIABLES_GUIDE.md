# Global Variables Guide

## Visão Geral

O sistema agora suporta variáveis globais por fluxo. Essas variáveis podem ser:
- Criadas e gerenciadas na UI
- Atribuídas valores usando o novo passo "SetVariable"
- Usadas em templates de texto com a sintaxe `{{NomeVariavel}}`

## Funcionalidades

### 1. Criar Variáveis Globais

Na tela de edição do fluxo, existe uma seção "Variáveis Globais" onde você pode:
- **Adicionar nova variável**: Digite o nome e valor, depois clique "➕ Adicionar"
- **Remover variável**: Clique "🗑️" ao lado da variável para remover
- **Editar variável**: Clique na linha da variável para editar diretamente

Exemplo:
```
Nome: usuario
Valor: João Silva

Nome: email
Valor: joao@example.com

Nome: id_cliente
Valor: 12345
```

### 2. Passo SetVariable

Use este passo para **atribuir valores dinâmicos** às variáveis.

**Propriedades:**
- **Label**: Nome descritivo (ex: "Extrair ID do resultado")
- **Nome da Variável**: Nome da variável a ser atribuída (sem ${})
- **Valor**: Pode usar:
  - Valores literais: `"João Silva"`
  - Colunas CSV: `{{Coluna}}`
  - Outras variáveis: `{{OutraVariavel}}`
  - Combinações: `"Cliente {{Cliente}} - ID {{id}}"`

### 3. Usar Variáveis em TypeText

Em qualquer ação "Digitar Texto", você pode usar:
- `{{usuario}}` - valor da variável "usuario"
- `{{email}}` - valor da variável "email"
- Combinar com CSV: `{{usuario}}_{{Coluna}}`

## Exemplo Prático

### Cenário: Extrair dados de uma página e preencher formulário

**Fluxo:**

1. **SetVariable - Extrair URL**
   - Nome: `url_base`
   - Valor: `https://example.com/api`

2. **SetVariable - Extrair Token**
   - Nome: `token`
   - Valor: `{{URLFromPage}}`  (de um elemento capturado)

3. **TypeText - Preencher Header**
   - Template: `Authorization: Bearer {{token}}`

4. **SetVariable - Concatenar URL**
   - Nome: `endpoint_completo`
   - Valor: `{{url_base}}/users/{{id_cliente}}`

5. **TypeText - Digitar URL**
   - Template: `{{endpoint_completo}}`

### Resultado na Execução

```
Variáveis iniciais:
  usuario = "João Silva"
  email = "joao@example.com"
  id_cliente = "12345"

[SetVariable] token = "abc123xyz"
[SetVariable] endpoint_completo = "https://example.com/api/users/12345"

[TypeText] Authorization: Bearer abc123xyz
[TypeText] https://example.com/api/users/12345
```

## Persistência

As variáveis globais são **salvas no arquivo do fluxo** (`.rpaflow.json`):

```json
{
  "name": "Meu Fluxo",
  "globalVariables": {
    "usuario": "João Silva",
    "email": "joao@example.com",
    "id_cliente": "12345"
  },
  "steps": [...]
}
```

Quando o fluxo é carregado, todas as variáveis são restauradas.

## Escopo das Variáveis

### Por Iteração

Cada iteração (linha CSV ou repetição) **inicia com as variáveis definidas no fluxo**.

Modificações durante a execução não afetam a próxima iteração:

```
Iteração 1:
  usuario = "João"
  SetVariable usuario = "Carlos"  ← modificação
  TypeText {{usuario}} → digita "Carlos"

Iteração 2:
  usuario = "João"  ← volta ao valor inicial
  SetVariable usuario = "Maria"
  TypeText {{usuario}} → digita "Maria"
```

### Exemplo Completo

**Entrada CSV:**
```
Nome,Email
João,joao@example.com
Maria,maria@example.com
```

**Fluxo:**
```
1. SetVariable - FullName
   Valor: {{Nome}}_{{usuario}}

2. TypeText
   Template: {{FullName}} ({{Email}})
```

**Resultado:**
```
Iteração 1: João_admin (joao@example.com)
Iteração 2: Maria_admin (maria@example.com)
```

## Resolvendo Placeholders

A ordem de resolução é:
1. **Variáveis CSV** (linhas da planilha)
2. **Variáveis Globais** (definidas no fluxo)
3. **Variáveis SetVariable** (criadas durante execução)

Se houver conflito, a **variável local sobrescreve a global**.

## Casos de Uso

### 1. Credentials
```
usuario = admin
senha = senha123

[TypeText] {{usuario}}
[TypeText] {{senha}}
```

### 2. Endpoints
```
api_url = https://api.example.com/v2
recurso = /users

[TypeText] {{api_url}}{{recurso}}
```

### 3. Extrair e Reutilizar
```
[SetVariable] id_extraido = "12345"
[TypeText] /api/items/{{id_extraido}}
[TypeText] {{id_extraido}}  ← reutiliza o valor
```

### 4. Transformações
```
[SetVariable] full_name = {{first_name}} {{last_name}}
[TypeText] Bem-vindo, {{full_name}}!
```

## Logging

Variáveis globais aparecem no log de execução:

```
[EXEC] Variáveis globais: usuario, email, id_cliente
[EXEC]   SetVariableStep: Atribuindo $token
[EXEC]     $token = "abc123xyz"
```

## Limitações e Notas

- ✅ Nomes de variáveis são **case-sensitive**: `Usuario` ≠ `usuario`
- ✅ Caracteres especiais em valores são **escapados automaticamente**
- ✅ Valores vazios são permitidos
- ❌ Variáveis não persistem **entre fluxos** diferentes
- ❌ Não suporta expressões matemáticas (use scripts/PowerShell se necessário)

## Boas Práticas

1. **Nomes descritivos**: `usuario` em vez de `u`
2. **Valores iniciais significativos**: Deixe `""` se não souber o valor inicial
3. **SetVariable no início**: Defina variáveis no topo do fluxo para clareza
4. **Comentários no Label**: Use Label = "SetVariable - Extrair ID do formulário"
5. **Teste sem CSV**: Primeiro teste com valores hardcoded nas variáveis globais

