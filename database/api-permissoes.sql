-- =============================================================================
-- Permissões do usuário de banco da API (chateau_api)
--
-- A API do app conecta com um usuário próprio, separado do portal web, com o
-- mínimo necessário. O Row Level Security segue ligado em todas as tabelas
-- (veja schema.sql), então cada permissão precisa também de uma policy.
--
-- Rode depois de criar o usuário (troque a senha):
--   CREATE ROLE chateau_api WITH LOGIN PASSWORD '<senha forte>' NOINHERIT;
-- Pode ser rodado de novo sempre que as permissões mudarem.
--
-- Os testes de integração rodam a API com este mesmo usuário e estas mesmas
-- permissões, então uma permissão faltando quebra os testes antes da produção.
-- =============================================================================

GRANT USAGE ON SCHEMA public TO chateau_api;

-- Leitura: vagas, empresas, currículos e login dos três tipos de usuário
GRANT SELECT ON administrativo, empresa, profissional, experiencia, certificado, vaga, candidatos TO chateau_api;

-- Profissional: candidatar-se e cancelar
GRANT INSERT, DELETE ON candidatos TO chateau_api;
GRANT USAGE ON SEQUENCE candidatos_idcandidatos_seq TO chateau_api;

-- Empresa: publicar, editar e excluir vagas (a API só deixa mexer nas da própria empresa)
GRANT INSERT, UPDATE, DELETE ON vaga TO chateau_api;
GRANT USAGE ON SEQUENCE vaga_idvaga_seq TO chateau_api;

-- Administrador: excluir profissionais e empresas. Currículos, vagas e candidaturas
-- dependentes saem pelo ON DELETE CASCADE, que o PostgreSQL executa como dono das tabelas.
GRANT DELETE ON profissional, empresa TO chateau_api;

-- Sem acesso a TipoUser nem a DataProtectionKeys (chaves de login do portal web)

DO $$
DECLARE t TEXT;
BEGIN
    FOREACH t IN ARRAY ARRAY['administrativo','empresa','profissional','experiencia','certificado','vaga','candidatos'] LOOP
        EXECUTE format('DROP POLICY IF EXISTS api_leitura ON %I', t);
        EXECUTE format('CREATE POLICY api_leitura ON %I FOR SELECT TO chateau_api USING (true)', t);
    END LOOP;
END $$;

DROP POLICY IF EXISTS api_candidatar ON candidatos;
CREATE POLICY api_candidatar ON candidatos FOR INSERT TO chateau_api WITH CHECK (true);
DROP POLICY IF EXISTS api_cancelar ON candidatos;
CREATE POLICY api_cancelar ON candidatos FOR DELETE TO chateau_api USING (true);

DROP POLICY IF EXISTS api_publicar ON vaga;
CREATE POLICY api_publicar ON vaga FOR INSERT TO chateau_api WITH CHECK (true);
DROP POLICY IF EXISTS api_editar ON vaga;
CREATE POLICY api_editar ON vaga FOR UPDATE TO chateau_api USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS api_excluir ON vaga;
CREATE POLICY api_excluir ON vaga FOR DELETE TO chateau_api USING (true);

DROP POLICY IF EXISTS api_excluir ON profissional;
CREATE POLICY api_excluir ON profissional FOR DELETE TO chateau_api USING (true);
DROP POLICY IF EXISTS api_excluir ON empresa;
CREATE POLICY api_excluir ON empresa FOR DELETE TO chateau_api USING (true);
